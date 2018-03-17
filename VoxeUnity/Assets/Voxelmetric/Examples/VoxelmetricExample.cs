using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.Operations;
using Voxelmetric.Code.Core.Serialization;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Examples
{
    public class VoxelmetricExample : MonoBehaviour
    {
        public World world;
        public Camera cam;
        private Vector2 rot;

        public string blockToPlace = "air";
        public Text selectedBlockText;
        public Text saveProgressText;

        private Vector3Int pfStart;
        private Vector3Int pfStop;
        public PathFinder pf;

        private SaveProgress saveProgress;
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
            if (saveProgress != null && saveProgress.GetProgress() >= 100)
                saveProgress = null;

            // Roatation
            if (Input.GetMouseButton(1))
            {
                rot = new Vector2(
                    rot.x+Input.GetAxis("Mouse X")*3,
                    rot.y+Input.GetAxis("Mouse Y")*3
                    );

                cam.transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
                cam.transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);
            }

            // Movement
            float speedModificator = 1f;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speedModificator = 2f;
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                speedModificator = 0.25f;
            cam.transform.position += cam.transform.forward*40f*speedModificator*Input.GetAxis("Vertical")*Time.deltaTime;
            cam.transform.position += cam.transform.right*40f*speedModificator*Input.GetAxis("Horizontal")*Time.deltaTime;

            // Screenspace mouse cursor coordinates
            var mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

            if (world!=null)
            {
                Block block = world.blockProvider.GetBlock(blockToPlace);
                VmRaycastHit hit = VmRaycast.Raycast(
                    new Ray(cam.transform.position, mousePos-cam.transform.position),
                    world, 100, block.Type==BlockProvider.AirType
                    );

                // Display the type of the selected block
                if (selectedBlockText!=null)
                    selectedBlockText.text = Code.Voxelmetric.GetBlock(world, ref hit.vector3Int).DisplayName;

                // Save current world status
                if (saveProgressText != null)
                    saveProgressText.text = saveProgress != null ? SaveStatus() : "Save";

                if (eventSystem!=null && !eventSystem.IsPointerOverGameObject())
                {
                    if (hit.block.Type!=BlockProvider.AirType)
                    {
                        bool adjacent = block.Type!=BlockProvider.AirType;
                        Vector3Int blockPos = adjacent ? hit.adjacentPos : hit.vector3Int;
                        Debug.DrawLine(cam.transform.position, blockPos, Color.red);
                    }

                    // Clicking voxel blocks
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (hit.block.Type!=BlockProvider.AirType)
                        {
                            bool adjacent = block.Type!=BlockProvider.AirType;
                            Vector3Int blockPos = adjacent ? hit.adjacentPos : hit.vector3Int;
                            Code.Voxelmetric.SetBlock(world, ref blockPos, new BlockData(block.Type, block.Solid));
                        }
                    }

                    // Pathfinding
                    if (Input.GetKeyDown(KeyCode.I))
                    {
                        if (hit.block.Type!=BlockProvider.AirType)
                        {
                            bool adjacent = block.Type!=BlockProvider.AirType;
                            pfStart = adjacent ? hit.adjacentPos : hit.vector3Int;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.O))
                    {
                        if (hit.block.Type!=BlockProvider.AirType)
                        {
                            bool adjacent = block.Type!=BlockProvider.AirType;
                            pfStop = adjacent ? hit.adjacentPos : hit.vector3Int;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.P))
                    {
                        pf = new PathFinder(pfStart, pfStop, world, 0);
                    }

                    if (pf!=null && pf.path.Count!=0)
                    {
                        for (int i = 0; i<pf.path.Count-1; i++)
                        {
                            Vector3 p0 = (Vector3)pf.path[i]+Env.HalfBlockOffset;
                            Vector3 p1 = (Vector3)pf.path[i+1]+Env.HalfBlockOffset;
                            Debug.DrawLine(p0, p1, Color.red);
                        }
                    }
                }

                // Test of ranged block setting
                if (Input.GetKeyDown(KeyCode.T))
                {
                    Action<ModifyBlockContext> action = context => { Debug.Log("Action performed"); };

                    Vector3Int fromPos = new Vector3Int(-44,-44,-44);
                    Vector3Int toPos = new Vector3Int(44, 44, 44);
                    Code.Voxelmetric.SetBlockRange(world, ref fromPos, ref toPos, BlockProvider.AirBlock, action);
                }
            }
        }
        
        public void SaveAll()
        {
            if (saveProgress != null)
                return;

            saveProgress = new SaveProgress(
                Code.Voxelmetric.SaveAll(world)
                );
        }

        public string SaveStatus()
        {
            if (saveProgress == null)
                return "";

            return saveProgress.GetProgress().ToString() + "%";
        }

    }
}
