using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GridironFieldPainter : MonoBehaviour
{
    public enum HashMarkStandard { NFL, College }

    [Header("🏈 Regulatory Field Settings")]
    public HashMarkStandard ruleStandard = HashMarkStandard.NFL;
    public float fieldWidthYards = 53.33f;
    public float fieldLengthYards = 100f;
    public float endzoneDepthYards = 10f;

    [Header("🎨 Material Assets")]
    public Material chalkMaterial;

    [Header("🏟️ Endzone Cosmetics (Arrowhead Style)")]
    public Color endzonePaintColor = new Color(0.7f, 0.1f, 0.1f); // Arrowhead Red
    public bool paintEndzonesCustomColor = true;

    [Space(5)]
    public string endzoneTextString = "END ZONE";
    public Color endzoneTextColor = Color.black;
    [Range(1f, 30f)] public float endzoneTextSize = 18f;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    void Start()
    {
        InitializeFieldPainting();

        GoalpostGenerator goalpostGen = GetComponent<GoalpostGenerator>();
        if (goalpostGen != null)
        {
            goalpostGen.ExecuteSafeGeneration(fieldLengthYards, endzoneDepthYards);
        }
    }

    private void InitializeFieldPainting()
    {
        string[] tagsToClear = { "Standalone_Chalk_Overlay", "Procedural_Endzone", "Field_Text_Marker" };
        foreach (Transform child in transform)
        {
            foreach (string tag in tagsToClear)
            {
                if (child.name.Contains(tag) || child.name.Contains("Yard_") || child.name.Contains("EZ_") || child.name.Contains("Digit_") || child.name.Contains("Arrow"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        Vector3 inverseScale = new Vector3(1f / transform.localScale.x, 1f / transform.localScale.y, 1f / transform.localScale.z);

        if (paintEndzonesCustomColor)
        {
            CreateProceduralEndzonePlanes(inverseScale);
        }

        GameObject overlayHolder = new GameObject("Standalone_Chalk_Overlay");
        overlayHolder.transform.parent = this.transform;
        overlayHolder.transform.localPosition = Vector3.zero;
        overlayHolder.transform.localRotation = Quaternion.identity;
        overlayHolder.transform.localScale = inverseScale;

        MeshFilter mf = overlayHolder.AddComponent<MeshFilter>();
        MeshRenderer mr = overlayHolder.AddComponent<MeshRenderer>();

        if (chalkMaterial == null)
        {
            chalkMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            chalkMaterial.color = Color.white;
        }
        mr.material = chalkMaterial;

        GenerateFullFieldChalkGeometry();

        Mesh mesh = new Mesh();
        mesh.name = "Procedural_Chalk_Mesh";
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        GenerateFieldNumbers(overlayHolder.transform);
        GenerateEndzoneText(overlayHolder.transform);
    }

    private void CreateProceduralEndzonePlanes(Vector3 invScale)
    {
        float totalLength = fieldLengthYards + (endzoneDepthYards * 2f);
        float halfLength = totalLength * 0.5f;

        Material ezMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        ezMat.color = endzonePaintColor;

        GameObject southEZ = GameObject.CreatePrimitive(PrimitiveType.Quad);
        southEZ.name = "Procedural_Endzone_South";
        southEZ.transform.parent = this.transform;
        southEZ.transform.localPosition = new Vector3(0f, 0.01f, -halfLength + (endzoneDepthYards * 0.5f));
        southEZ.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        southEZ.transform.localScale = new Vector3(fieldWidthYards * invScale.x, endzoneDepthYards * invScale.z, 1f);
        Destroy(southEZ.GetComponent<MeshCollider>());
        southEZ.GetComponent<Renderer>().material = ezMat;

        GameObject northEZ = GameObject.CreatePrimitive(PrimitiveType.Quad);
        northEZ.name = "Procedural_Endzone_North";
        northEZ.transform.parent = this.transform;
        northEZ.transform.localPosition = new Vector3(0f, 0.01f, halfLength - (endzoneDepthYards * 0.5f));
        northEZ.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        northEZ.transform.localScale = new Vector3(fieldWidthYards * invScale.x, endzoneDepthYards * invScale.z, 1f);
        Destroy(northEZ.GetComponent<MeshCollider>());
        northEZ.GetComponent<Renderer>().material = ezMat;
    }

    private void GenerateFullFieldChalkGeometry()
    {
        float totalLengthYards = fieldLengthYards + (endzoneDepthYards * 2f);
        float halfWidth = fieldWidthYards * 0.5f;
        float interiorLineWidth = 0.11f;
        float boundaryLineWidth = 0.45f;
        float yHeight = 0.015f;

        float halfLength = totalLengthYards * 0.5f;
        float southEndlineZ = -halfLength;
        float northEndlineZ = halfLength;
        float southGoalLineZ = southEndlineZ + endzoneDepthYards;
        float northGoalLineZ = northEndlineZ - endzoneDepthYards;

        CreateMeshQuad(new Vector3(-halfWidth - boundaryLineWidth, yHeight, southEndlineZ), new Vector3(-halfWidth, yHeight, southEndlineZ),
                       new Vector3(-halfWidth - boundaryLineWidth, yHeight, northEndlineZ), new Vector3(-halfWidth, yHeight, northEndlineZ));
        CreateMeshQuad(new Vector3(halfWidth, yHeight, southEndlineZ), new Vector3(halfWidth + boundaryLineWidth, yHeight, southEndlineZ),
                       new Vector3(halfWidth, yHeight, northEndlineZ), new Vector3(halfWidth + boundaryLineWidth, yHeight, northEndlineZ));

        CreateHorizontalLine(southEndlineZ, halfWidth, boundaryLineWidth, yHeight);
        CreateHorizontalLine(northEndlineZ, halfWidth, boundaryLineWidth, yHeight);
        CreateHorizontalLine(southGoalLineZ, halfWidth, interiorLineWidth, yHeight);
        CreateHorizontalLine(northGoalLineZ, halfWidth, interiorLineWidth, yHeight);

        for (int yard = 1; yard < fieldLengthYards; yard++)
        {
            float lineZCoord = southGoalLineZ + yard;
            if (yard % 5 == 0) CreateHorizontalLine(lineZCoord, halfWidth, interiorLineWidth, yHeight);
            CreateHashMarksForYard(lineZCoord, halfWidth, interiorLineWidth, yHeight, (yard % 5 == 0));
        }
    }

    private void CreateHorizontalLine(float zCoord, float halfWidth, float lineWidth, float yHeight)
    {
        CreateMeshQuad(new Vector3(-halfWidth, yHeight, zCoord - (lineWidth * 0.5f)),
                       new Vector3(halfWidth, yHeight, zCoord - (lineWidth * 0.5f)),
                       new Vector3(-halfWidth, yHeight, zCoord + (lineWidth * 0.5f)),
                       new Vector3(halfWidth, yHeight, zCoord + (lineWidth * 0.5f)));
    }

    private void CreateHashMarksForYard(float zCoord, float halfWidth, float lineWidth, float yHeight, bool isMajorYard)
    {
        float hashOffset = (ruleStandard == HashMarkStandard.NFL) ? 3.08f : 6.66f;
        float tickLength = 0.66f; 
        float boundaryTickLength = 0.66f; 

        CreateMeshQuad(new Vector3(-hashOffset - (tickLength * 0.5f), yHeight, zCoord - (lineWidth * 0.5f)),
                       new Vector3(-hashOffset + (tickLength * 0.5f), yHeight, zCoord - (lineWidth * 0.5f)),
                       new Vector3(-hashOffset - (tickLength * 0.5f), yHeight, zCoord + (lineWidth * 0.5f)),
                       new Vector3(-hashOffset + (tickLength * 0.5f), yHeight, zCoord + (lineWidth * 0.5f)));
        CreateMeshQuad(new Vector3(hashOffset - (tickLength * 0.5f), yHeight, zCoord - (lineWidth * 0.5f)),
                       new Vector3(hashOffset + (tickLength * 0.5f), yHeight, zCoord - (lineWidth * 0.5f)),
                       new Vector3(hashOffset - (tickLength * 0.5f), yHeight, zCoord + (lineWidth * 0.5f)),
                       new Vector3(hashOffset + (tickLength * 0.5f), yHeight, zCoord + (lineWidth * 0.5f)));

        if (!isMajorYard)
        {
            float leftInwardEdge = -halfWidth + boundaryTickLength;
            CreateMeshQuad(new Vector3(-halfWidth, yHeight, zCoord - (lineWidth * 0.5f)), new Vector3(leftInwardEdge, yHeight, zCoord - (lineWidth * 0.5f)),
                           new Vector3(-halfWidth, yHeight, zCoord + (lineWidth * 0.5f)), new Vector3(leftInwardEdge, yHeight, zCoord + (lineWidth * 0.5f)));
            float rightInwardEdge = halfWidth - boundaryTickLength;
            
            // 👇 FIXED LINE BELOW: Changed the invalid 'boundaryLineWidth' back to 'lineWidth' parameter argument
            CreateMeshQuad(new Vector3(rightInwardEdge, yHeight, zCoord - (lineWidth * 0.5f)), new Vector3(halfWidth, yHeight, zCoord - (lineWidth * 0.5f)),
                           new Vector3(rightInwardEdge, yHeight, zCoord + (lineWidth * 0.5f)), new Vector3(halfWidth, yHeight, zCoord + (lineWidth * 0.5f)));
        }
    }

    private void GenerateFieldNumbers(Transform parentContainer)
    {
        // 👇 FIXED: Changed 'endzoneDepthYARDS' to lowercase 'endzoneDepthYards'
        float totalLength = fieldLengthYards + (endzoneDepthYards * 2f); 
        float halfLength = totalLength * 0.5f;
        float southGoalLineZ = -halfLength + endzoneDepthYards;
        float halfWidth = fieldWidthYards * 0.5f;

        float distanceFromSideline = (ruleStandard == HashMarkStandard.NFL) ? 12f : 9f;
        float numberXPosition = halfWidth - distanceFromSideline;

        for (int yard = 10; yard <= 90; yard += 10)
        {
            float zCoord = southGoalLineZ + yard;
            int displayValue = (yard <= 50) ? yard : 100 - yard;

            string tensDigit = (displayValue / 10).ToString();
            string onesDigit = (displayValue % 10).ToString();

            // Corrected Mirror Perspective Left-to-Right Coordinates
            CreateSingleDigitMesh(tensDigit, new Vector3(-numberXPosition, 0.03f, zCoord + 0.70f), parentContainer, false);
            CreateSingleDigitMesh(onesDigit, new Vector3(-numberXPosition, 0.03f, zCoord - 0.70f), parentContainer, false);

            CreateSingleDigitMesh(tensDigit, new Vector3(numberXPosition, 0.03f, zCoord - 0.70f), parentContainer, true);
            CreateSingleDigitMesh(onesDigit, new Vector3(numberXPosition, 0.03f, zCoord + 0.70f), parentContainer, true);

            // VERTICAL GOAL-LINE ARROWS POSITION OVERRIDE:
            if (yard != 50)
            {
                bool pointsNorth = (yard > 50);

                // Place arrows perfectly inline with the digits on the X-axis (numberXPosition).
                // Shift along the length Z-axis beyond the outer digit toward the closest endzone.
                float arrowZOffset = 2.1f;
                float finalArrowZ = pointsNorth ? (zCoord + arrowZOffset) : (zCoord - arrowZOffset);

                CreateProceduralYardArrow(new Vector3(-numberXPosition, 0.03f, finalArrowZ), pointsNorth, parentContainer, false);
                CreateProceduralYardArrow(new Vector3(numberXPosition, 0.03f, finalArrowZ), pointsNorth, parentContainer, true);
            }
        }
    }

    private void CreateSingleDigitMesh(string digit, Vector3 position, Transform parent, bool isRightSide)
    {
        GameObject numObj = new GameObject($"Digit_{digit}");
        numObj.transform.parent = parent;
        numObj.transform.localPosition = position;
        numObj.transform.localRotation = Quaternion.Euler(90f, isRightSide ? -90f : 90f, 0f);

        TextMeshPro tmp = numObj.AddComponent<TextMeshPro>();
        tmp.text = digit;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform rect = numObj.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector3(1.33f, 2.0f, 1f);
    }

    private void CreateProceduralYardArrow(Vector3 position, bool pointsNorth, Transform parent, bool isRightSide)
    {
        GameObject arrowObj = new GameObject("Yard_GoalLine_Arrow");
        arrowObj.transform.parent = parent;
        arrowObj.transform.localPosition = position;

        // 📐 ROTATION MATH FLIP:
        // Removed the extra +180f offset that was accidentally mirroring the triangles backward.
        // This forces them to point directly toward their respective endzones!
        float baseYaw = isRightSide ? -90f : 90f;
        float arrowDirectionYaw = pointsNorth ? (isRightSide ? 90f : -90f) : (isRightSide ? -90f : 90f);

        arrowObj.transform.localRotation = Quaternion.Euler(90f, baseYaw + arrowDirectionYaw, 0f);

        TextMeshPro tmp = arrowObj.AddComponent<TextMeshPro>();
        tmp.text = "▲";
        tmp.fontSize = 12f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform rect = arrowObj.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector3(1.5f, 1.5f, 1f);
    }

    private void GenerateEndzoneText(Transform parentContainer)
    {
        float totalLength = fieldLengthYards + (endzoneDepthYards * 2f);
        float halfLength = totalLength * 0.5f;
        float southTextZ = -halfLength + (endzoneDepthYards * 0.5f);
        float northTextZ = halfLength - (endzoneDepthYards * 0.5f);

        CreateEndzoneTextBillboard(endzoneTextString, new Vector3(0f, 0.04f, southTextZ), parentContainer, false);
        CreateEndzoneTextBillboard(endzoneTextString, new Vector3(0f, 0.04f, northTextZ), parentContainer, true);
    }

    private void CreateEndzoneTextBillboard(string text, Vector3 position, Transform parent, bool rotated)
    {
        GameObject txtObj = new GameObject("EZ_Text_Billboard");
        txtObj.transform.parent = parent;
        txtObj.transform.localPosition = position;
        txtObj.transform.localRotation = Quaternion.Euler(90f, rotated ? 180f : 0f, 0f);

        TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = endzoneTextSize * 3f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = endzoneTextColor;

        RectTransform rect = txtObj.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector3(fieldWidthYards - 4f, endzoneDepthYards - 2f, 1f);
    }

    private void CreateMeshQuad(Vector3 bl, Vector3 br, Vector3 tl, Vector3 tr)
    {
        int vIndex = vertices.Count;
        vertices.Add(bl); vertices.Add(br); vertices.Add(tl); vertices.Add(tr);
        triangles.Add(vIndex); triangles.Add(vIndex + 2); triangles.Add(vIndex + 1);
        triangles.Add(vIndex + 1); triangles.Add(vIndex + 2); triangles.Add(vIndex + 3);
    }
}