using UnityEngine;
using UnityEngine.UI;
using Voxelmetric.Code.Common.Math;

namespace Voxelmetric.Examples
{
    public class FrustumExample : MonoBehaviour
    {
        public Camera cam;
        public GameObject obj;
        
        private Renderer objRenderer;
        private Text txt;

        private Vector2 rot;
        private readonly Plane[] planes = new Plane[6];
        
        void Start()
        {
            rot.y = 360f - cam.transform.localEulerAngles.x;
            rot.x = cam.transform.localEulerAngles.y;

            objRenderer = obj.GetComponent<Renderer>();
            txt = FindObjectOfType<Text>();
        }

        void Update()
        {
            // Rotation
            if (Input.GetMouseButton(1))
            {
                rot = new Vector2(
                    rot.x+Input.GetAxis("Mouse X")*3,
                    rot.y+Input.GetAxis("Mouse Y")*3
                    );

                cam.transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
                cam.transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);
            }

            //Movement
            bool turbo = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            cam.transform.position += cam.transform.forward*40*(turbo ? 3 : 1)*Input.GetAxis("Vertical")*Time.deltaTime;
            cam.transform.position += cam.transform.right*40*(turbo ? 3 : 1)*Input.GetAxis("Horizontal")*Time.deltaTime;

            if (objRenderer!=null && txt!=null)
            {
                Planes.CalculateFrustumPlanes(cam, planes);
                int intersection = Planes.TestPlanesAABB2(planes, objRenderer.bounds);
                switch (intersection)
                {
                    case 0:
                        txt.text = "Outside";
                        break;
                    case 6:
                        txt.text = "Inside";
                        break;
                    default:
                        txt.text = "PartialyInside";
                        break;
                }
            }
        }
    }
}
