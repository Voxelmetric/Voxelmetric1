using System;

namespace Voxelmetric.Code.VM
{
    /// <summary>
    /// Handles recovery of information streamed over TCP back into messages of known size
    /// </summary>
    public class VmSocketState {

        public interface IMessageHandler {
            int GetExpectedSize(byte messageType);
            void HandleMessage(byte[] message);
        }

        public byte[] buffer = new byte[VmNetworking.bufferLength];

        private byte[] message = new byte[VmNetworking.bufferLength];
        private int messageOffset = 0;
        private int expectedSize = 0;
        private bool tmpExpectedSize;

        private IMessageHandler messageHandler;

        public VmSocketState(IMessageHandler messageHandler) {
            this.messageHandler = messageHandler;
            ResetMessage();
        }

        public void Receive(int received, int bufferOffset) {
            if (messageOffset == 0) {
                expectedSize = messageHandler.GetExpectedSize(buffer[bufferOffset]);
                if (expectedSize < 0) {
                    expectedSize = -expectedSize;
                    tmpExpectedSize = true;
                }
            }

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
                    //TODO So that small chunks don't need 1025 bytes to be sent...
                }
                // Message complete -- dispatch it!
                messageHandler.HandleMessage(message);
                ResetMessage(); // get ready for the next message
            }
            if (messageExtra > 0) {
                bufferOffset += toCopy;
                Receive(messageExtra, bufferOffset);
            }
        }

        private void ResetMessage() {
            messageOffset = 0;
            expectedSize = 0;
        }
    }
}
