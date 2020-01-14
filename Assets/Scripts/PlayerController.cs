using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController : Singleton<PlayerController>
{
    // Input variables
    float hPrev = 0f;
    float vPrev = 0f;
    int dirBuffer = 0;

    // Lane variables
    int currentLane = 0;
    int prevLane = 0;
    float laneWidth;
    public float LaneWidth { get { return laneWidth; } }
    Coroutine currentLaneChange;
    int laneChangeStack = 0;

    // Animation
    Animator anim;
    int jumpParam;
    int slideParam;

    // States
    enum State
    {
        Run = 0,
        Jump,
        Slide,

        Count
    }

    State state = State.Run;

    // Inspector parameters
    [SerializeField]
    int numLanes = 3;
    public int NumLanes { get { return numLanes; } }

    [SerializeField]
    float strafeSpeed = 5f; // 1/strafeSpeed = amount of time for lane change (seconds) 

    // Jump Parameters
    [SerializeField]
    float g = -9.81f;

    [SerializeField]
    float Vi = 5f;

    // Use this for initialization
    void Awake()
    {
        transform.position = Vector3.zero; // middle lane is always at origin
        laneWidth = 7.5f / numLanes;

        // Animation initialization
        anim = GetComponent<Animator>();
        jumpParam = Animator.StringToHash("Jump");
        slideParam = Animator.StringToHash("Slide");
    }

    // Update is called once per frame
    void Update()
    {
        float hNew = Input.GetAxisRaw(InputNames.horizontalAxis); // returns -1, 0 or 1 with no smoothing
        float vNew = Input.GetAxisRaw(InputNames.verticalAxis);
        float hDelta = hNew - hPrev;
        float vDelta = vNew - vPrev;

        if (Mathf.Abs(hDelta) > 0f && Mathf.Abs(hNew) > 0f && state == State.Run)
        {
            MovePlayer((int)hNew);
        }

        int v = 0;
        if (Mathf.Abs(vDelta) > 0f)
        {
            v = (int)vNew;
        }

        // Jumping
        if ((Input.GetButtonDown(InputNames.jumpButton) || v == 1) && state == State.Run)
        {
            state = State.Jump;
            StartCoroutine(Jump());
        }

        // Sliding
        if ((Input.GetButtonDown(InputNames.slideButton) || v == -1) && state == State.Run)
        {
            state = State.Slide;
            anim.SetTrigger(slideParam);
        }

        hPrev = hNew;
        vPrev = vNew;
    }

    void MovePlayer(int dir)
    {
        if (currentLaneChange != null)
        {
            if (currentLane + dir != prevLane)
            {
                dirBuffer = dir;
                return;
            }

            // override previous movement
            dirBuffer = 0;
            StopCoroutine(currentLaneChange);
        }

        prevLane = currentLane;
        currentLane = Mathf.Clamp(currentLane + dir, numLanes / -2, numLanes / 2);

        currentLaneChange = StartCoroutine(LaneChange());
    }

    // Strafe movement coroutine
    IEnumerator LaneChange()
    {
        Vector3 From = Vector3.right * prevLane * laneWidth;
        Vector3 To = Vector3.right * currentLane * laneWidth;

        float t = (laneWidth - Vector3.Distance(transform.position.x * Vector3.right, To)) / laneWidth;
        for (; t < 1f; t += strafeSpeed * Time.deltaTime / laneWidth)
        {
            transform.position = Vector3.Lerp(From + Vector3.up * transform.position.y, To + Vector3.up * transform.position.y, t);

            yield return null;
        }

        transform.position = To + Vector3.up * transform.position.y;
        currentLaneChange = null;

        if (dirBuffer != 0 && ++laneChangeStack <= 2)
        {
            MovePlayer(dirBuffer);
            dirBuffer = 0;
        }

        laneChangeStack = 0;
    }


    // Jumping coroutine
    IEnumerator Jump()
    {
        // Animation
        anim.SetBool(jumpParam, true);

        // Calculate total time of jump
        float tFinal = (Vi * 2f) / -g;

        // Calculate transition time
        float tLand = tFinal - 0.125f;

        float t = Time.deltaTime;

        for (; t < tLand; t += Time.deltaTime)
        {
            float y = g * (t * t) / 2f + Vi * t;
            Helpers.SetPositionY(transform, y);

            yield return null;
        }

        // Transition back to run
        anim.SetBool(jumpParam, false);
        state = State.Run;

        for (; t < tFinal; t += Time.deltaTime)
        {
            float y = g * (t * t) / 2f + Vi * t;
            Helpers.SetPositionY(transform, y);

            yield return null;
        }

        Helpers.SetPositionY(transform, 0f);
    }

    void FinishSlide()
    {
        state = State.Run;
    }

    public void Reset()
    {
        gameObject.SetActive(true);
        currentLane = 0;
        transform.position = Vector3.zero;
    }

}
