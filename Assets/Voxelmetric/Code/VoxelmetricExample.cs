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

        RaycastHit hit;
        var mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

        if (Input.GetMouseButtonDown(0))
        {

            bool adjacent = true;
            if (((Block)blockToPlace).type == Block.Air.type)
            {
                adjacent = false;
            }

            if (Physics.Raycast(Camera.main.transform.position, mousePos - Camera.main.transform.position, out hit, 100))
            {
                Voxelmetric.SetBlock(hit, blockToPlace, adjacent);
            }
        }

        if (Physics.Raycast(Camera.main.transform.position, mousePos - Camera.main.transform.position, out hit, 100))
        {
            selectedBlockText.text = Voxelmetric.GetBlock(hit).ToString();
        }

        if (saveProgress != null)
        {
            saveProgressText.text = SaveStatus();
        }
        else
        {
            saveProgressText.text = "Save";
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
             pf = new PathFinder(pfStart, pfStop, World.instance, 2);
            Debug.Log(pf.path.Count);
        }

        if (pf!=null && pf.path.Count != 0)
        {
            for (int i = 0; i < pf.path.Count - 1; i++)
                Debug.DrawLine(pf.path[i].Add(0,1,0), pf.path[i + 1].Add(0,1,0));
        }
    }

    public void SaveAll()
    {
        saveProgress = Voxelmetric.SaveAll();
    }

    public string SaveStatus()
    {
        if (saveProgress == null)
            return "";

        return saveProgress.GetProgress() + "%";
    }

}
