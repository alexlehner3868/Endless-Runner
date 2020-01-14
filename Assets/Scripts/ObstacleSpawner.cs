using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    GameObject[] LoadedObstacles;

    // unused distance from previous piece
    float leftover = 0;

    // Inspector Parameters
    [SerializeField]
    float distInterval = 35f;

    // Use this for initialization
    void Awake()
    {
        // Load obstacle prefabs
        LoadedObstacles = Resources.LoadAll<GameObject>("Obstacles");

        // Subscribe obstacle placing method to Road Manager's add piece event
        RoadManager.Instance.OnAddPiece += PlaceObstacles;
    }

    void PlaceObstacles(GameObject Piece)
    {
        Transform BeginLeft = Piece.transform.Find("BeginLeft");
        Transform BeginRight = Piece.transform.Find("BeginRight");
        Transform EndLeft = Piece.transform.Find("EndLeft");
        Transform EndRight = Piece.transform.Find("EndRight");

        // Get new piece length
        float length;

        // Curved piece variables
        Vector3 RotationPoint = Vector3.zero;
        float radius = 0f;

        if (Piece.tag == Tags.straightPiece)
        {
            length = Vector3.Distance(BeginLeft.position, EndLeft.position);
        }
        else
        {
            // Get radius
            RotationPoint = RoadManager.Instance.GetRotationPoint(BeginLeft, BeginRight, EndLeft, EndRight);
            radius = Vector3.Distance(Piece.transform.position, RotationPoint);

            // Get angle
            float angle = Vector3.Angle(BeginLeft.position - BeginRight.position, EndLeft.position - EndRight.position);

            length = radius * angle * Mathf.Deg2Rad;
        }

        float halfLength = length / 2f;

        float currDist = distInterval - halfLength - leftover;

        if (currDist >= halfLength)
        {
            leftover += halfLength * 2f;
        }

        for (; currDist < halfLength; currDist += distInterval)
        {
            // Obstacle container
            GameObject ObstacleRow = new GameObject("ObstacleRow");
            ObstacleRow.transform.position = Piece.transform.position;
            ObstacleRow.transform.rotation = Piece.transform.rotation;
            ObstacleRow.transform.Rotate(90f, 0f, 0f); // compensate for road piece rotation
            ObstacleRow.transform.parent = Piece.transform;


            bool consecutive = false;
            int prevIndex = -1;

            for (int i = PlayerController.Instance.NumLanes / -2; i <= PlayerController.Instance.NumLanes / 2; i++)
            {
                // Prevent 3 of the same obstacle in a row
                int randomObstacle = Random.Range(0, 3);
                if (randomObstacle == prevIndex)
                {
                    if (!consecutive)
                        consecutive = true;
                    else
                        randomObstacle = ++randomObstacle % LoadedObstacles.Length;
                }
                else
                {
                    consecutive = false;
                }
                prevIndex = randomObstacle;

                // Instantiate obstacle prefab
                GameObject Obstacle = Instantiate(LoadedObstacles[randomObstacle], ObstacleRow.transform.position, ObstacleRow.transform.rotation, ObstacleRow.transform);

                // move into correct lane
                Obstacle.transform.Translate(Vector3.right * i * PlayerController.Instance.LaneWidth, Space.Self);
            }


            if (Piece.tag == Tags.straightPiece)
            {
                ObstacleRow.transform.Translate(0f, 0f, currDist);
            }
            else
            {
                float angle = currDist / radius;
                ObstacleRow.transform.RotateAround(RotationPoint, Vector3.up, angle * Mathf.Rad2Deg * -Mathf.Sign(Piece.transform.localScale.x));
            }

            leftover = halfLength - currDist;
        }
    }
}
