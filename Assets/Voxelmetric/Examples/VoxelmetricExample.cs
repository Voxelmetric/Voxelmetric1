using UnityEngine;
using UnityEngine.UI;

public class VoxelmetricExample : MonoBehaviour
{
    Vector2 rot;

    public string blockToPlace = "air";
    public Text selectedBlockText;
    public Text saveProgressText;

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

        //Blocks
        RaycastHit hit;

        var mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));
        if (Physics.Raycast(Camera.main.transform.position, mousePos - Camera.main.transform.position, out hit, 100))
        {
            Chunk chunk = hit.collider.GetComponent<Chunk>();

            if (chunk == null)
            {
                return;
            }

            selectedBlockText.text = Voxelmetric.GetBlock(hit).displayName;

            if (Input.GetMouseButtonDown(0))
            {

                bool adjacent = true;
                if (Block.New(blockToPlace, chunk.world).type == Block.Air.type)
                {
                    adjacent = false;
                }

                if (Physics.Raycast(Camera.main.transform.position, mousePos - Camera.main.transform.position, out hit, 100))
                {
                    // Creates a game object block at the click pos:
                    // Voxelmetric.CreateGameObjectBlock(Voxelmetric.GetBlockPos(hit, adjacent), hit.collider.gameObject.GetComponent<Chunk>().world, hit.point, new Quaternion());
                    Voxelmetric.SetBlock(hit, Block.New(blockToPlace, chunk.world), adjacent);
                }
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (Physics.Raycast(Camera.main.transform.position, mousePos - Camera.main.transform.position, out hit, 100))
                {
                    pfStart = Voxelmetric.GetBlockPos(hit);
                }
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                if (Physics.Raycast(Camera.main.transform.position, mousePos - Camera.main.transform.position, out hit, 100))
                {
                    pfStop = Voxelmetric.GetBlockPos(hit);
                }
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                pf = new PathFinder(pfStart, pfStop, Voxelmetric.resources.worlds[0], 2);
                Debug.Log(pf.path.Count);
            }

            if (pf != null && pf.path.Count != 0)
            {
                for (int i = 0; i < pf.path.Count - 1; i++)
                    Debug.DrawLine(pf.path[i].Add(0, 1, 0), pf.path[i + 1].Add(0, 1, 0));
            }
        }
    }

    public void SaveAll()
    {
        saveProgress = Voxelmetric.SaveAll(Voxelmetric.resources.worlds[0]);
    }

    public string SaveStatus()
    {
        if (saveProgress == null)
            return "";

        return saveProgress.GetProgress() + "%";
    }

}
