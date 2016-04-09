using UnityEngine;
using UnityEngine.UI;

public class VoxelmetricExample : MonoBehaviour
{
    Vector2 rot;

    public string blockToPlace = "air";
    public Text selectedBlockText;
    public Text saveProgressText;
    public World world;

    BlockPos pfStart;
    BlockPos pfStop;
    public PathFinder pf;

    SaveProgress saveProgress;

    public void SetType(string newType){
        blockToPlace = newType;
    }

    void Update()
    {
        //Movement
        if (Input.GetMouseButton(1))
        {
            rot = new Vector2(
                rot.x + Input.GetAxis("Mouse X") * 3,
                rot.y + Input.GetAxis("Mouse Y") * 3);

            transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);
        }
        transform.position += transform.forward * 40 * Input.GetAxis("Vertical") * Time.deltaTime;
        transform.position += transform.right * 40 * Input.GetAxis("Horizontal") * Time.deltaTime;

        //Save
        if (saveProgress != null)
        {
            saveProgressText.text = SaveStatus();
        }
        else
        {
            saveProgressText.text = "Save";
        }

        var mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

        VmRaycastHit hit = Voxelmetric.Raycast(new Ray(Camera.main.transform.position, mousePos - Camera.main.transform.position), world, 1000);

        selectedBlockText.text = Voxelmetric.GetBlock(hit.blockPos, world).displayName;

        if (Input.GetMouseButtonDown(0))
        {
            if (hit.block.type != Block.VoidType)
            {
                bool adjacent = true;
                if (Block.New(blockToPlace, world).type == Block.AirType)
                {
                    adjacent = false;
                }

                if (adjacent)
                {
                    Voxelmetric.SetBlock(hit.adjacentPos, Block.New(blockToPlace, world), world);
                }
                else
                {
                    Voxelmetric.SetBlock(hit.blockPos, Block.New(blockToPlace, world), world);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
                pfStart = hit.blockPos;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            pfStop = hit.blockPos;
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

    }

    public void SaveAll()
    {
        saveProgress = Voxelmetric.SaveAll(world);
    }

    public string SaveStatus()
    {
        if (saveProgress == null)
            return "";

        return saveProgress.GetProgress() + "%";
    }

}
