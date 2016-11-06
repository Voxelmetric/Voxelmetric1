using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Handles recovery of information streamed over TCP back into messages of known size
/// </summary>
public class VmSocketState {

    public interface IMessageHandler {
        /// <summary>
        /// Expected size of the message.
        /// If positive then the message has a fixed size.
        /// If negative then the negation is the offset of an integer that gives the size of this instance.
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        int GetExpectedSize(byte messageType);
        void HandleMessage(byte[] message);
    }

    public byte[] buffer = new byte[VmNetworking.bufferLength];

    private byte[] message = new byte[VmNetworking.bufferLength];
    private int messageOffset = 0;
    private int expectedSize = 0;
    private bool tmpExpectedSize;

    private IMessageHandler messageHandler;

    private bool debug = false;

    public VmSocketState(IMessageHandler messageHandler) {
        this.messageHandler = messageHandler;
        ResetMessage();
    }

    public void Receive(int received) {
        Receive(received, 0);
    }

    private void Receive(int received, int bufferOffset) {
        if ( debug ) Debug.Log("VmSocketState.Receive: received=" + received + " at " + bufferOffset);

        if (messageOffset == 0)
            SetExpectedSize(messageHandler.GetExpectedSize(buffer[bufferOffset]));

        int messageEnd = messageOffset + received;
        int messageExtra = 0;
        if (messageEnd > expectedSize) {
            messageExtra = messageEnd - expectedSize;
            messageEnd = expectedSize;
        }
        int toCopy = received - messageExtra;
        Array.Copy(buffer, bufferOffset, message, messageOffset, toCopy);
        messageOffset += toCopy;
        if (messageOffset == expectedSize) {
            if (tmpExpectedSize) {
                int realExpectedSize = BitConverter.ToInt32(message, messageOffset - 4);
                if (debug) Debug.Log("VmSocketState.Receive: realExpectedSize=" + realExpectedSize);
                SetExpectedSize(realExpectedSize);
                int remaining = received - toCopy;
                if ( remaining >= 0 )
                    Receive(remaining, bufferOffset + toCopy);
                return;
            } else {
                // Message complete -- dispatch it!
                messageHandler.HandleMessage(message);
                ResetMessage(); // get ready for the next message
            }
        }
        if (messageExtra > 0) {
            bufferOffset += toCopy;
            Receive(messageExtra, bufferOffset);
        }
    }

    private void SetExpectedSize(int newExpectedSize) {
        expectedSize = newExpectedSize;
        if (expectedSize < 0) {
            expectedSize = 4 - expectedSize; // To get to the end of the integer with the real size
            tmpExpectedSize = true;
        } else {
            tmpExpectedSize = false;
        }
    }

    private void ResetMessage() {
        messageOffset = 0;
        SetExpectedSize(0);
    }
}
