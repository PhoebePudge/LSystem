using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System; 
public class PlantGen : MonoBehaviour {
    //dol system
    //leaf
    //fenestration
    //leaf colours
    //fix leaf positions
    //rule switching
    //joints
    //fix up variable uses

    [Header("Plant Generation")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private bool DOLSystem = true;
    [SerializeField] private float angle = 25.000f;

    [Header("Pillaring")]
    [SerializeField] public bool pillarGeneration = true;
    [SerializeField] [Range(1, 4)] public int generationPerPillar = 2;
    [SerializeField] public int pillarHeight = 3;

    [Header("Branch Generation Settings")]
    [SerializeField] private bool enforceStartingBranch = true;
    [SerializeField] [Range(0,2.0f)] private float branchLength = 10.0f; private float _branchLength;
    [SerializeField] [Range(1,15)]private float banchThicknessDivider = 8;
    [SerializeField] [Range(1, 4)] private int generationHeight = 5; 
    [SerializeField] [Range(1,2)] private float branchMultiplier = 1.0f; private float _branchMultiplier; 
    [SerializeField] [Range(0, 1)] private float lengthVariance = 0.10f;
    [SerializeField] [Range(0, 100)] private int BranchingChance = 50;
    [SerializeField] [Range(0.1f, 1f)] private float RandomMultiplier = 0.1f;

    [Header("Plant Apperance")]
    [SerializeField] private Color Col1 = new Color(255 / 21, 255 / 33, 255 / 52, 1);
    [SerializeField] private Color Col2 = new Color(255 / 133, 255 / 174, 255 / 142, 1);
    [SerializeField] private bool FlipColour = false;
    [SerializeField] private bool SolidColour = true;
    [Header("Material")] 
    [SerializeField] [Range(0.03f, 1)] float MaterialShinyness = 0.078125f;
    [SerializeField] [Range(0f,1f)] float  MaterialEmision = 1;
    [SerializeField] private SpriteRenderer spriteRenderer;

    //rules
    private const char axiom = 'F';
    private string currentString = string.Empty;
    [SerializeField] private string dictionaryString = "GF+[+F-F-F]-[--F+F+F]";
    private Dictionary<char, string> rules;

    //mesh
    private List<MeshFilter> meshObjectList;
    private GameObject MeshObject;
    private GameObject a_Leaves;
    private Sprite sprite;
    private Texture2D texture;

    //transform
    TransformInfo previous; 
    bool Validate = false;
    private Stack<TransformInfo> transformStack;
    private Vector3 initialPosition;
    private Vector3 Origin; 
    GameObject TreeDrawer;

    //Leaf generation
    int colourDiv = 2;
    int positionMulti = 65;
    int positionOff;
    private List<Vector3> leafVerts = new List<Vector3>();

    //General
    private bool Validation() { if (branchMultiplier == 0) return false; else return true; }
    private void OnValidate() { if (Application.isPlaying & Validation()) { Validate = true; } }
    private void Start(){
        TreeDrawer = new GameObject("TreeDrawer"); TreeDrawer.tag = "Validate";
        texture = new Texture2D(128,128);
        sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),100f);
        spriteRenderer.sprite = sprite;

        for (int x = 0; x < texture.width; x++) 
            for (int y = 0; y < texture.height; y++) 
                texture.SetPixel(x,y,Color.white); 

        a_Leaves = new GameObject("Leaves");
        rules = new Dictionary<char, string> { { axiom, dictionaryString } };
        transform.Rotate(Vector3.right * -90.0f);
        TreeDrawer.transform.rotation = transform.rotation;
        Origin = transform.position;
    }
    private void Make(){

        TreeDrawer = new GameObject("TreeDrawer");
        TreeDrawer.transform.rotation = transform.rotation;
        TreeDrawer.transform.position = Origin;
        //transform.position = Origin;

        if (MeshObject != null)  
            Destroy(MeshObject); 

        meshObjectList = new List<MeshFilter>();
        _branchLength = branchLength;
        _branchMultiplier = 1;

        foreach (GameObject item in GameObject.FindGameObjectsWithTag("Validate")) 
            Destroy(item); 
        
        transformStack = new Stack<TransformInfo>();
        currentString = axiom.ToString(); 

        if (enforceStartingBranch) 
            currentString += "[FF]"; 

        if (pillarGeneration) {
            //perform the numbers of generation in a pillar
            for (int i = 0; i < generationPerPillar; i++) 
                Append(ref currentString, rules); 
            //add this pillar for the height
            for (int i = 0; i < pillarHeight; i++) 
                currentString += currentString; 
            Gen();
        } else { 
            for (int i = 0; i < generationHeight; i++) 
                Append(ref currentString, rules); 
            Gen();
        }
        CombineMeshes();
        GenerateLeaves();
    }
    private void Update(){
        if (Validate){
            Validate = false;
            Make();
        }
        if (rotate) 
            transform.Rotate(Vector3.forward * (Time.deltaTime * 20f)); 
    }














    //Branch
    private void Append(ref string s,  Dictionary<char, string> rule){
        StringBuilder sb = new StringBuilder();
        if (BranchingChance != 0 & s != axiom.ToString()){
            foreach (char c in s){
                if (rule.ContainsKey(c)){
                    if (UnityEngine.Random.Range(0,100) > BranchingChance){
                        sb.Append(rule[c]);}                    
                } else {
                    sb.Append(c.ToString());} }
        }else{
            foreach (char c in s){
                sb.Append(rules.ContainsKey(c) ? rule[c] : c.ToString());}}
        s = sb.ToString();
    }
    private Vector3 Move(Vector3 direction, float length){
        TreeDrawer.transform.Translate(Vector3.forward * (length + (UnityEngine.Random.Range(0, lengthVariance * 100f) / 100f)));
        return TreeDrawer.transform.position;
    }

    //Branch Mesh
    private Material GetMaterial() {
        Material m = Resources.Load<Material>("Standard");
        m.SetFloat("_Emission", MaterialEmision);
        m.SetFloat("_Shininess", MaterialShinyness); 
        return m;
    }
    private void CombineMeshes(){
        MeshObject = new GameObject("MeshObject", typeof(MeshFilter), typeof(MeshRenderer));
        MeshObject.GetComponent<MeshRenderer>().material = GetMaterial();
        List<Color> colours = new List<Color>(); 
        CombineInstance[] combine = new CombineInstance[meshObjectList.Count];
        int i = 0;
        while (i < meshObjectList.Count){
            if (meshObjectList[i] != null){
                combine[i].mesh = meshObjectList[i].sharedMesh;
                combine[i].transform = meshObjectList[i].transform.localToWorldMatrix;
                for (int d = 0; d < combine[i].mesh.vertexCount; d++)  { 
                    colours.Add(meshObjectList[i].GetComponent<MeshRenderer>().material.color); 
                }; 
                Destroy(meshObjectList[i].gameObject);
            }
            i++;
        }
        
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);
        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateNormals();
        combinedMesh.colors = colours.ToArray();
        MeshObject.GetComponent<MeshFilter>().mesh = combinedMesh;
        MeshObject.transform.SetParent(transform);

        foreach (Vector3 n in combinedMesh.normals)
        {
            Debug.Log(n.ToString());
        } 
    } 
    private void Gen() {
        TransformInfo highestLeaf = new TransformInfo();
        foreach (char c in currentString) {
            TransformInfo current = new TransformInfo(
                new TransformHolder(TreeDrawer.transform), 
                _branchLength * _branchMultiplier, null);
            float l = _branchLength + (UnityEngine.Random.Range(0, lengthVariance * 100f) / 100f);
            switch (c) {
                case 'F': 
                    _branchLength /= branchMultiplier; 
                    DrawBranch(current.transform.position, Move(Vector3.forward, l), _branchLength, out current); 
                    previous = current;
                    break;

                case 'B':
                    _branchLength /= branchMultiplier; 
                    DrawBranch(current.transform.position, Move(-Vector3.forward, l), _branchLength, out current);
                    previous = current;
                    break;

                case 'G': TreeDrawer.transform.Rotate(Vector3.forward * UnityEngine.Random.Range(0, 90)); break;
                case 'H': break;

                case '+': TreeDrawer.transform.Rotate(Vector3.up * angle); break; //pitch
                case '-': TreeDrawer.transform.Rotate(Vector3.up * -angle); break; //pitch

                case '{': TreeDrawer.transform.Rotate(Vector3.right * angle); break; //yaw
                case '}': TreeDrawer.transform.Rotate(Vector3.right * -angle); break; //yaw

                case '<': TreeDrawer.transform.Rotate(Vector3.forward * angle); break; //roll
                case '>': TreeDrawer.transform.Rotate(Vector3.forward * -angle); break; //roll

                case '[': 
                    transformStack.Push(new TransformInfo(current.transform, _branchLength, current.mesh));
                    GenerateLeaf(highestLeaf);
                    break;

                case ']': 
                    highestLeaf = current;
                    current = transformStack.Pop(); 
                    SetTransform(ref TreeDrawer, current); 
                    _branchLength = current.branchLength;
                    break;

                default: throw new InvalidOperationException("Invalid L-tree operation");
            }
        }
    }
    private Color ColourMaterial(Color Col1, Color Col2) {
        if (SolidColour)
            return Col1;
        else
            return Color.Lerp(Col2, Col1, transformStack.Count / 3f);
    }
    private void DrawBranch(Vector3 pA, Vector3 pB, float length, out TransformInfo ti){
        GameObject gm = new GameObject("Branch", typeof(MeshFilter), typeof(MeshRenderer));
        gm.GetComponent<MeshFilter>().mesh = Rectangle();
        if (FlipColour) gm.GetComponent<MeshRenderer>().material.color = ColourMaterial(Col2, Col1);
        else gm.GetComponent<MeshRenderer>().material.color = ColourMaterial(Col1, Col2); 
        Vector3 between = pB - pA; 
        float thicknessMultiplier = (float)(UnityEngine.Random.Range(0f, 5f) * (float)RandomMultiplier) / 50f;  
        gm.transform.localScale = new Vector3((length) / banchThicknessDivider + thicknessMultiplier, (length) / banchThicknessDivider + thicknessMultiplier, (between.magnitude ));
        gm.transform.localPosition = pA + (between / 2.0f);
        gm.transform.LookAt(pB);
        gm.tag = "Validate"; 
        ti = new TransformInfo( new TransformHolder(gm.transform), length, gm.GetComponent<MeshFilter>().mesh); 
        meshObjectList.Add(gm.GetComponent<MeshFilter>());
    } 
    private Mesh Rectangle(){
        List<Vector3> vertices = new List<Vector3>{
        new Vector3 (-0.5f , -0.5f, -0.5f ),    new Vector3 ( 0.5f , -0.5f, -0.5f ),
        new Vector3 ( 0.5f ,  0.5f, -0.5f ),    new Vector3 (-0.5f ,  0.5f, -0.5f ),
        new Vector3 (-0.5f ,  0.5f,  0.5f ),    new Vector3 ( 0.5f ,  0.5f,  0.5f ),
        new Vector3 ( 0.5f , -0.5f,  0.5f ),    new Vector3 (-0.5f , -0.5f,  0.5f ),};
        List<int> triangles =  new List<int>{
        2, 3, 4, 2, 4, 5,
        1, 2, 5, 1, 5, 6,
        0, 7, 4, 0, 4, 3,
        0, 6, 7, 0, 1, 6};
        Mesh msh = new Mesh();
        msh.vertices = vertices.ToArray();
        msh.triangles = triangles.ToArray(); 
        msh.RecalculateBounds();
        msh.RecalculateNormals();
        msh.name = "Rect";
        return msh;
    }
    private void SetTransform(ref GameObject a, TransformInfo b) {
        a.transform.position = b.transform.position;
        a.transform.rotation = b.transform.rotation;
    }



































    //Leaf 
    public void DrawCircle(ref Texture2D tex, Color color, int x, int y, int radius = 3) {
        float rSquared = radius * radius;
        for (int u = x - radius; u < x + radius + 1; u++)
            for (int v = y - radius; v < y + radius + 1; v++)
                if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                    tex.SetPixel(u, v, color); 
    }
    private void GenerateLeaf(TransformInfo ti) {
        positionOff = (texture.width / 2) - (25);
        GameObject gm = new GameObject("Leaf");
        SetTransform(ref gm, ti);
        gm.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        gm.tag = "Validate";
        gm.transform.SetParent(a_Leaves.transform);
        Vector3 v3 = MapFromWorldSpace(gm);
        DrawCircle(ref texture, new Color(ColourFloat(gm), ColourFloat(gm), ColourFloat(gm)), (int)v3.x, (int)v3.z);
        texture.Apply();
    }
    private float ColourFloat(GameObject gm) { return gm.transform.position.y / colourDiv; }
    private void GenerateLeaves() {
        for (int x = 0; x < texture.width; x++) {
            for (int y = 0; y < texture.height; y++) {

            }
        }
    }
    Vector3 MapFromWorldSpace(GameObject gm) {
        return new Vector3(
            positionOff + ((gm.transform.position.x - gameObject.transform.position.x) * positionMulti),
            0,
            positionOff + ((gm.transform.position.z - gameObject.transform.position.z) * positionMulti));
    }
    Vector2 WorldSpaceFromMap(Vector3 m) {
        return new Vector3(
            (((m.x / positionOff) / positionMulti) + gameObject.transform.position.x),
            0,
            (((m.z / positionOff) / positionMulti) + gameObject.transform.position.z)
            );
    }

    //Utilities 
    Mesh Sphere() {
        GameObject gm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh m = gm.GetComponent<MeshFilter>().mesh;
        Destroy(gm);
        return m;
    }
    GameObject DrawSphere(Vector3 pos, float size, Color c) {
        GameObject g = new GameObject("Giz " + c, typeof(MeshRenderer), typeof(MeshFilter));
        g.AddComponent<MeshFilter>().mesh = Sphere();
        g.GetComponent<MeshRenderer>().material.color = c;
        g.transform.position = pos;
        g.transform.localScale = new Vector3(size, size, size);
        return g;
    }
}