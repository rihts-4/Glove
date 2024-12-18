
using System;
using System.Collections;
using System.Collections.Concurrent; // For thread-safe queue
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class grab : MonoBehaviour
{
    public string esp32WebSocketUrl = "ws://192.168.0.12:81"; // Replace with your ESP32 IP and WebSocket port
    private WebSocket webSocket;

    private float left_index_degree = 0;
    private float left_middle_degree = 0;
    private float left_ring_degree = 0;
    private float left_thumb_degree = 0;
    private float left_pinky_degree = 0;

    private GameObject[] left_index;
    private GameObject[] left_pinky;
    private GameObject[] left_ring;
    private GameObject[] left_middle;
    private GameObject[] left_thumb;
    private GameObject left_hand;

    private float[] left_hand_trans = new float[] { 0.0f, 0.0f, 0.0f };
    private float[] left_hand_rot = new float[] { 0.0f, 0.0f, 0.0f };

    private float val;
    public GameObject cube;
    public List<GameObject> listofCubes = new List<GameObject>();

    private bool isConnected = false;

    // Thread-safe queue for main thread actions
    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private float ClampRotation(float value, float min, float max)
    {
        return Mathf.Clamp(value, min, max);
    }


    void Start()
    {
        InitializeHandParts();
        InitializeWebSocket();
    }

    void Update()
    {
        // Process all actions queued for the main thread
        while (mainThreadActions.TryDequeue(out var action))
        {
            action?.Invoke();
        }

        if (isConnected)
        {
            ProcessCubes();
        }
    }

    // Initialize hand parts
    private void InitializeHandParts()
    {
        left_index = GameObject.FindGameObjectsWithTag("left_index");
        left_pinky = GameObject.FindGameObjectsWithTag("left_pinky");
        left_ring = GameObject.FindGameObjectsWithTag("left_ring");
        left_middle = GameObject.FindGameObjectsWithTag("left_middle");
        left_thumb = GameObject.FindGameObjectsWithTag("left_thumb");
        left_hand = GameObject.FindWithTag("left_hand");
    }

    // Initialize WebSocket connection
    private void InitializeWebSocket()
    {
        webSocket = new WebSocket(esp32WebSocketUrl);

        webSocket.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket connected.");
            isConnected = true;
        };

        webSocket.OnMessage += (sender, e) =>
        {
            Debug.Log("Received message: " + e.Data);

            // Enqueue the message processing to the main thread
            mainThreadActions.Enqueue(() =>
            {
                try
                {
                    string[] serialOutputs = e.Data.Split(':');
                    if (serialOutputs.Length >= 3 && float.TryParse(serialOutputs[2], out val))
                    {
                        ProcessData(serialOutputs);
                    }
                    else
                    {
                        Debug.LogWarning("Invalid data format received.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error processing data: " + ex.Message);
                }
            });
        };

        webSocket.OnError += (sender, e) =>
        {
            Debug.LogError("WebSocket error: " + e.Message);
        };

        webSocket.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket closed.");
            isConnected = false;
        };

        Debug.Log("Connecting to WebSocket...");
        webSocket.Connect();
    }

    // Process parsed data
    // Process parsed data
    private void ProcessData(string[] serialOutputs)
    {
        if (serialOutputs[0] == "Left")
        {
            switch (serialOutputs[1])
            {
                case "Index":
                    val = ClampRotation(val, -30f, 120f); // Lock rotation to 0° - 90°
                    RotateFinger(left_index, val - left_index_degree);
                    left_index_degree = val;
                    break;

                case "Middle":
                    val = ClampRotation(val, 0f, 90f);
                    RotateFinger(left_middle, val - left_middle_degree);
                    left_middle_degree = val;
                    break;

                case "Ring":
                    val = ClampRotation(val, -30f, 120f);
                    RotateFinger(left_ring, val - left_ring_degree);
                    left_ring_degree = val;
                    break;

                case "Pinky":
                    val = ClampRotation(val, -30f, 120f);
                    RotateFinger(left_pinky, val - left_pinky_degree);
                    left_pinky_degree = val;
                    break;

                case "Thumb":
                    val = ClampRotation(val, -30f, 110f);
                    RotateFinger(left_thumb, val - left_thumb_degree);
                    left_thumb_degree = val;
                    break;


                case "RotateX":
                    RotateHand(left_hand, val - left_hand_rot[0], 0, 0); // Adjust axis mapping
                    left_hand_rot[0] = val;
                    break;
                case "RotateY":
                    RotateHand(left_hand, 0, val - left_hand_rot[1], 0);
                    left_hand_rot[1] = val;
                    break;
                case "RotateZ":
                    RotateHand(left_hand, 0, 0, val - left_hand_rot[2]);
                    left_hand_rot[2] = val;
                    break;



                case "TranslateX":
                    // Translate along X-axis; scaling adjusted for ESP32 range
                    TranslateHand(left_hand, val / 3000.0f, 0, 0);
                    left_hand_trans[0] = val;
                    break;

                case "TranslateY":
                    // Translate along Y-axis
                    TranslateHand(left_hand, 0, (val - left_hand_trans[1]) / 3000.0f, 0);
                    left_hand_trans[1] = val;
                    break;

                case "TranslateZ":
                    // Translate along Z-axis
                    TranslateHand(left_hand, 0, 0, (val - left_hand_trans[2]) / 3000.0f);
                    left_hand_trans[2] = val;
                    break;

                default:
                    Debug.LogWarning($"Unknown data type received: {serialOutputs[1]}");
                    break;
            }
        }
    }


    // Finger rotation
    private void RotateFinger(GameObject[] bones, float rotationValue)
    {
        foreach (GameObject bone in bones)
        {
            bone.transform.Rotate(0, 0, rotationValue * -70 / 180);
        }
    }

    // Hand rotation
    private void RotateHand(GameObject hand, float xRotation, float yRotation, float zRotation)
    {
        hand.transform.Rotate(xRotation, yRotation, zRotation);
    }

    // Hand translation
    private void TranslateHand(GameObject hand, float xTranslation, float yTranslation, float zTranslation)
    {
        hand.transform.Translate(xTranslation, yTranslation, zTranslation);
    }

    // Handle cube interactions
    private void ProcessCubes()
    {
        if (left_ring_degree > 40) CreateCube(-1, 2, 4);
        if (left_index_degree > 40) CreateCube(1, 2, 4);
        if (left_middle_degree > 40) CreateCube(0, 2, 4);

        if (left_middle_degree > 60 && left_index_degree > 60 && left_ring_degree > 60 && left_thumb_degree > 60)
        {
            DestroyCubes();
        }

        if (left_hand_rot[2] > 20)
        {
            ReverseGravity();
        }
    }

    // Create a cube
    private void CreateCube(int x, int y, int z)
    {
        GameObject newCube = Instantiate(cube, new Vector3(x, y, z), Quaternion.identity);
        listofCubes.Add(newCube);
    }

    // Destroy all cubes
    private void DestroyCubes()
    {
        foreach (GameObject cube in listofCubes)
        {
            Destroy(cube);
        }
        listofCubes.Clear();
    }

    // Reverse gravity
    private void ReverseGravity()
    {
        foreach (GameObject cube in listofCubes)
        {
            cube.GetComponent<Rigidbody>().AddForce(0, 30F, 0);
        }
    }

    void OnDestroy()
    {
        if (webSocket != null && webSocket.IsAlive)
        {
            webSocket.Close();
        }
    }
}
