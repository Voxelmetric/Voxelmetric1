using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.Serialization;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Examples
{
    public class VoxelmetricExample : MonoBehaviour
    {
        Vector2 rot;

        public string blockToPlace = "air";
        public Text selectedBlockText;
        public Text saveProgressText;
        public World world;
        public Camera cam;

        Vector3Int pfStart;
        Vector3Int pfStop;
        public PathFinder pf;

        SaveProgress saveProgress;

        private EventSystem eventSystem;

        public void SetType(string newType)
        {
            blockToPlace = newType;
        }

        void Start()
        {
            rot.y = 360f - cam.transform.localEulerAngles.x;
            rot.x = cam.transform.localEulerAngles.y;
            eventSystem = FindObjectOfType<EventSystem>();
        }

        void Update()
        {
            //Movement
            if (Input.GetMouseButton(1))
            {
                rot = new Vector2(
                    rot.x+Input.GetAxis("Mouse X")*3,
                    rot.y+Input.GetAxis("Mouse Y")*3
                    );

                cam.transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
                cam.transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);
            }

            bool turbo = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            cam.transform.position += cam.transform.forward * 40 * (turbo ? 3 : 1) * Input.GetAxis("Vertical") * Time.deltaTime;
            cam.transform.position += cam.transform.right * 40 * (turbo ? 3 : 1) * Input.GetAxis("Horizontal") * Time.deltaTime;

            //Save
            saveProgressText.text = saveProgress != null ? SaveStatus() : "Save";

            var mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

            ushort type = world.blockProvider.GetType(blockToPlace);
            VmRaycastHit hit = Code.Voxelmetric.Raycast(new Ray(cam.transform.position, mousePos - cam.transform.position), world, 100, type==BlockProvider.AirType);

            selectedBlockText.text = Code.Voxelmetric.GetBlock(world, hit.vector3Int).displayName;

            // Clicking voxel blocks
            if (Input.GetMouseButtonDown(0) && !eventSystem.IsPointerOverGameObject())
            {
                if (hit.block.type != BlockProvider.AirType)
                {
                    bool adjacent = type != BlockProvider.AirType;
                    Code.Voxelmetric.SetBlock(world, adjacent ? hit.adjacentPos : hit.vector3Int, new BlockData(type));
                }
            }

            // Pathfinding
            if (Input.GetKeyDown(KeyCode.I))
            {
                pfStart = hit.vector3Int;
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                pfStop = hit.vector3Int;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                pf = new PathFinder(pfStart, pfStop, world, 2);
                Debug.Log(pf.path.Count);
            }

            if (pf != null && pf.path.Count != 0)
            {
                for (int i = 0; i < pf.path.Count - 1; i++)
                    Debug.DrawLine(pf.path[i].Add(0, 1, 0), pf.path[i + 1].Add(0, 1, 0));
            }

            // Test of ranged block setting
            if (Input.GetKeyDown(KeyCode.T))
            {
                Code.Voxelmetric.SetBlockRange(world, new Vector3Int(-44, -44, -44), new Vector3Int(44, 44, 44), new BlockData(1));
            }
        }

        public void SaveAll()
        {
            var chunksToSave = Code.Voxelmetric.SaveAll(world);
            saveProgress = new SaveProgress(chunksToSave);
        }

        public string SaveStatus()
        {
            if (saveProgress == null)
                return "";

            return saveProgress.GetProgress() + "%";
        }

    }
}
