using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : Singleton<RoadManager>
{
    GameObject[] LoadedPieces;
    List<GameObject> RoadPieces;

    Transform BeginLeft, BeginRight, EndLeft, EndRight;

    Vector3 RotationPoint = Vector3.zero;

    // Events
    public delegate void AddPieceHandler(GameObject Piece);
    public event AddPieceHandler OnAddPiece;

    // Inspector Parameters
    [SerializeField]
    int numPieces = 10;

    [SerializeField]
    string firstPieceFilename = "Straight60m";

    [SerializeField]
    float speed = 20f;

    // Use this for initialization
    void Start ()
    {
        LoadedPieces = Resources.LoadAll<GameObject>("RoadPieces");
        RoadPieces = new List<GameObject>();

        // Hardcode first two road pieces
        RoadPieces.Add(Instantiate(Resources.Load("RoadPieces/" + firstPieceFilename)) as GameObject);      
        RoadPieces.Add(Instantiate(Resources.Load("RoadPieces/" + firstPieceFilename)) as GameObject);
        Vector3 Displacement = RoadPieces[0].transform.Find("EndLeft").position - RoadPieces[1].transform.Find("BeginLeft").position;
        RoadPieces[1].transform.Translate(Displacement, Space.World);

        // Build initial road
        for (int i = 2; i < numPieces; i++)
        {
            AddPiece();
        }

        RoadPieces[0].transform.parent = RoadPieces[1].transform;

        // Move road to pass first piece
        float halfLength = (RoadPieces[0].transform.Find("BeginLeft").position - 
                            RoadPieces[0].transform.Find("EndLeft").position).magnitude / 2;
        RoadPieces[1].transform.Translate(0f, 0f, -halfLength, Space.World);

        SetCurrentPiece();
    }

    // Update is called once per frame
    void Update ()
    {
        MovePiece(speed * Time.deltaTime);

        if (EndLeft.position.z < 0f || EndRight.position.z < 0f)
        {
            // Snap current piece to x-axis
            float resetDistance = GetResetDistance();
            MovePiece(-resetDistance);

            CyclePieces();

            MovePiece(resetDistance);

            // force straight pieces to align with world axes
            if (RoadPieces[1].tag == Tags.straightPiece)
            {
                RoadPieces[1].transform.rotation = new Quaternion(RoadPieces[1].transform.rotation.x,
                                                                  0f,
                                                                  0f,
                                                                  RoadPieces[1].transform.rotation.w);

                RoadPieces[1].transform.position = new Vector3(0f, 0f, RoadPieces[1].transform.position.z);
            }
        }
    }

    void AddPiece()
    {
        int randomIndex = Random.Range(0, LoadedPieces.Length);

        RoadPieces.Add(Instantiate(LoadedPieces[randomIndex],
                                    RoadPieces[RoadPieces.Count - 1].transform.position,
                                    RoadPieces[RoadPieces.Count - 1].transform.rotation));

        // Get references to the two pieces we are processing
        Transform NewPiece = RoadPieces[RoadPieces.Count - 1].transform;
        Transform PrevPiece = RoadPieces[RoadPieces.Count - 2].transform;

        // Get positions of four corner GameObjects
        BeginLeft = NewPiece.Find("BeginLeft");
        EndLeft = PrevPiece.Find("EndLeft");
        BeginRight = NewPiece.Find("BeginRight");
        EndRight = PrevPiece.Find("EndRight");

        // Compute edges
        Vector3 BeginEdge = BeginRight.position - BeginLeft.position;
        Vector3 EndEdge = EndRight.position - EndLeft.position;

        // Compute angle between edges
        float angle = Vector3.Angle(BeginEdge, EndEdge) * Mathf.Sign(Vector3.Cross(BeginEdge, EndEdge).y);

        // Rotate new piece to align with previous piece
        NewPiece.Rotate(0f, angle, 0f, Space.World);

        // Move piece into position
        Vector3 Displacement = EndLeft.position - BeginLeft.position;
        NewPiece.Translate(Displacement, Space.World);

        // Parent the current road piece to all other pieces
        NewPiece.parent = RoadPieces[1].transform;

        OnAddPiece(NewPiece.gameObject);
    }
    
    public Vector3 GetRotationPoint(Transform BeginLeft, Transform BeginRight, Transform EndLeft, Transform EndRight)
    {
        // Compute edges from corner positions
        Vector3 BeginEdge = BeginLeft.position - BeginRight.position;
        Vector3 EndEdge = EndLeft.position - EndRight.position;

        float a = Vector3.Dot(BeginEdge, BeginEdge); // square magnitude of begin edge
        float b = Vector3.Dot(BeginEdge, EndEdge); // project BeginEdge onto EndEdge
        float e = Vector3.Dot(EndEdge, EndEdge); // square magnitude of end edge

        float d = a*e - b*b;

        Vector3 r = BeginLeft.position - EndLeft.position;
        float c = Vector3.Dot(BeginEdge, r);
        float f = Vector3.Dot(EndEdge, r);

        float s = (b*f - c*e) / d;
        float t = (a*f - c*b) / d;

        Vector3 RotationPointBegin = BeginLeft.position + BeginEdge * s; // where the begin edge will intersect the end
        Vector3 RotationPointEnd = EndLeft.position + EndEdge * t; // where the end edge will intersect the begin

        // return midpoint between two closest points
        return (RotationPointBegin + RotationPointEnd) / 2f;
    }

    void SetCurrentPiece()
    {
        // Get corner markers of current piece
        BeginLeft = RoadPieces[1].transform.Find("BeginLeft");
        BeginRight = RoadPieces[1].transform.Find("BeginRight");
        EndLeft = RoadPieces[1].transform.Find("EndLeft");
        EndRight = RoadPieces[1].transform.Find("EndRight");

        RotationPoint = GetRotationPoint(BeginLeft, BeginRight, EndLeft, EndRight);
    }

    void MovePiece(float distance)
    {
        if (RoadPieces[1].tag == Tags.straightPiece)
        {
            RoadPieces[1].transform.Translate(0f, 0f, -speed * Time.deltaTime, Space.World);
        }
        else
        {
            float radius = Mathf.Abs(RotationPoint.x);
            float angle = ((speed * Time.deltaTime) / radius) * Mathf.Sign(RoadPieces[1].transform.localScale.x) * Mathf.Rad2Deg;
            RoadPieces[1].transform.RotateAround(RotationPoint, Vector3.up, angle);
        }
    }

    // remove roadpiece at index0, add new piece to end
    void CyclePieces()
    {
        Destroy(RoadPieces[0]);
        RoadPieces.RemoveAt(0);
        AddPiece();

        // reparent all pieces
        for (int i = RoadPieces.Count - 1; i >= 0; i--)
        { // [1] must be unparented before [0] can be reparented, so iterate backwards
            RoadPieces[i].transform.parent = null;
            RoadPieces[i].transform.parent = RoadPieces[1].transform;
        }

        SetCurrentPiece();
    }

    // distance required move piece to align with world x-axis
    float GetResetDistance()
    {
        if (RoadPieces[1].tag == Tags.straightPiece)
        {
            return -EndLeft.transform.position.z;
        }
        else
        {
            Vector3 EndEdge = EndRight.position - EndLeft.position;
            float angle = Vector3.Angle(Vector3.right, EndEdge);
            float radius = Mathf.Abs(RotationPoint.x);
            return angle * Mathf.Deg2Rad * radius; // convert angle to radians and calculate angular velocity
        }
    }

    public void Reset()
    {
        enabled = true;

        // Destroy road pieces
        Destroy(RoadPieces[1]);
        RoadPieces.Clear();

        // Generate new road
        Start();
    }
}
