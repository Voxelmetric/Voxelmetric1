using UnityEngine;
using System.Collections;

public class Modify : MonoBehaviour
{

    Vector2 rot;

    public World world;
    public byte type;

    public void SetType(int newType){
        type = (byte)newType;
    }

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            rot = new Vector2(
                rot.x + Input.GetAxis("Mouse X") * 3,
                rot.y + Input.GetAxis("Mouse Y") * 3);

            transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);
        }
        transform.position += transform.forward * 50 * Input.GetAxis("Vertical") * Time.deltaTime;
        transform.position += transform.right * 50 * Input.GetAxis("Horizontal") * Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
           RaycastHit hit;
           var mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

           bool adjacent = 0 != type;
           if (Physics.Raycast(Camera.main.transform.position, mousePos - Camera.main.transform.position, out hit, 100))
           {
               Terrain.SetBlock(hit, new SBlock(type), adjacent);
           }
       }
    }
}
