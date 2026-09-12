using UnityEngine;
using System.Collections.Generic;

public class GoalpostGenerator : MonoBehaviour
{
    [Header("🎨 Color Cosmetics")]
    public Color postColor = new Color(1f, 0.85f, 0f);
    public Color padColor = new Color(0.15f, 0.15f, 0.15f);
    public Color flagColor = new Color(1f, 0.25f, 0f);

    [HideInInspector]
    public List<GameObject> activeWindFlags = new List<GameObject>();

    public void ExecuteSafeGeneration(float lengthYards, float endzoneDepthYards)
    {
        float halfTotalLengthYards = (lengthYards + (endzoneDepthYards * 2f)) * 0.5f;

        // Unparenting targets completely bypasses the field's Z=12 matrix distortion
        BuildSingleGoalpostAssembly(transform.position + new Vector3(0f, 0f, -halfTotalLengthYards), false);
        BuildSingleGoalpostAssembly(transform.position + new Vector3(0f, 0f, halfTotalLengthYards), true);
    }

    private void BuildSingleGoalpostAssembly(Vector3 worldPosition, bool rotateFacing)
    {
        GameObject goalpostRoot = new GameObject("Stadium_Goalpost_Assembly");
        goalpostRoot.transform.position = worldPosition;
        goalpostRoot.transform.rotation = rotateFacing ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        goalpostRoot.transform.localScale = Vector3.one; 

        Material yellowMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); yellowMat.color = postColor;
        Material padMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); padMat.color = padColor;
        Material flagMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); flagMat.color = flagColor;

        // 1. THE VERTICAL MAIN BASE POST (Sticks straight up out of the dirt)
        GameObject verticalPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        verticalPost.name = "Vertical_Base_Post"; verticalPost.transform.parent = goalpostRoot.transform;
        verticalPost.transform.localScale = new Vector3(0.25f, 1.66f, 0.25f);
        verticalPost.transform.localPosition = new Vector3(0f, 1.66f, -2.0f); // 2-yard offset back from endline
        verticalPost.GetComponent<Renderer>().material = yellowMat;

        // 2. THE PROTECTIVE CUSHION PAD
        GameObject protectivePad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        protectivePad.name = "Safety_Pad"; protectivePad.transform.parent = goalpostRoot.transform;
        protectivePad.transform.localScale = new Vector3(0.55f, 1.08f, 0.55f);
        protectivePad.transform.localPosition = new Vector3(0f, 1.08f, -2.0f);
        protectivePad.GetComponent<Renderer>().material = padMat;

        // 3. 🛠️ THE CONNECTING EXTENSION GOOSENECK ARM (Bridges the floating physical gap cleanly!)
        GameObject gooseneckExtension = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gooseneckExtension.name = "Gooseneck_Extension_Arm"; gooseneckExtension.transform.parent = goalpostRoot.transform;
        gooseneckExtension.transform.localScale = new Vector3(0.22f, 1.0f, 0.22f); // 2-yard length arm link
        gooseneckExtension.transform.localPosition = new Vector3(0f, 3.33f, -1.0f); 
        gooseneckExtension.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Rotated flush over Z axis
        gooseneckExtension.GetComponent<Renderer>().material = yellowMat;

        // 4. THE HORIZONTAL CROSSBAR
        GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crossbar.name = "Crossbar"; crossbar.transform.parent = goalpostRoot.transform;
        crossbar.transform.localScale = new Vector3(0.18f, 3.08f, 0.18f); // 18'6" Wide standard width
        crossbar.transform.localPosition = new Vector3(0f, 3.33f, 0.0f);   // Snapped flush to front of the arm
        crossbar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        crossbar.GetComponent<Renderer>().material = yellowMat;

        // 5. THE VERTICAL UPRIGHT SPEARS
        GameObject leftUpright = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftUpright.name = "Left_Upright"; leftUpright.transform.parent = goalpostRoot.transform;
        leftUpright.transform.localScale = new Vector3(0.15f, 5.83f, 0.15f);
        leftUpright.transform.localPosition = new Vector3(-3.08f, 9.16f, 0.0f);
        leftUpright.GetComponent<Renderer>().material = yellowMat;

        GameObject rightUpright = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightUpright.name = "Right_Upright"; rightUpright.transform.parent = goalpostRoot.transform;
        rightUpright.transform.localScale = new Vector3(0.15f, 5.83f, 0.15f);
        rightUpright.transform.localPosition = new Vector3(3.08f, 9.16f, 0.0f);
        rightUpright.GetComponent<Renderer>().material = yellowMat;

        // 6. ATTACH THE WIND FLAGS
        float flagTipY = 14.99f;
        GameObject leftFlag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftFlag.name = "Left_Flag"; leftFlag.transform.parent = goalpostRoot.transform;
        leftFlag.transform.localScale = new Vector3(0.04f, 0.11f, 0.33f);
        leftFlag.transform.localPosition = new Vector3(-3.08f, flagTipY, -0.16f);
        leftFlag.GetComponent<Renderer>().material = flagMat;
        Destroy(leftFlag.GetComponent<BoxCollider>());
        activeWindFlags.Add(leftFlag);

        GameObject rightFlag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightFlag.name = "Right_Flag"; rightFlag.transform.parent = goalpostRoot.transform;
        rightFlag.transform.localScale = new Vector3(0.04f, 0.11f, 0.33f);
        rightFlag.transform.localPosition = new Vector3(3.08f, flagTipY, -0.16f);
        rightFlag.GetComponent<Renderer>().material = flagMat;
        Destroy(rightFlag.GetComponent<BoxCollider>());
        activeWindFlags.Add(rightFlag);
    }
}