using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformInfo {
    public TransformInfo(TransformHolder _transform, float _length, Mesh _mesh){
        transform = _transform; 
        branchLength = _length; 
        mesh = _mesh;
    }
    public TransformInfo(){
        transform = new TransformHolder(); 
        branchLength = 0; 
        mesh = new Mesh();
    }
    public TransformHolder transform;
    public float branchLength;
    public Mesh mesh;
}
public class TransformHolder{
    //no paramiter creation
    public TransformHolder(){
        position = new Vector3(); 
        localScale = new Vector3(); 
        rotation = new Quaternion();
    }
    //invidudal paramiter creation
    public TransformHolder(Vector3 _Position, Vector3 _LocalScale, Quaternion _Rotation){
        position = _Position; 
        localScale = _LocalScale; 
        rotation = _Rotation;
    }
    //transform paramiter creation
    public TransformHolder(Transform transform) {
        position = transform.position;
        rotation = transform.rotation;
        localScale = transform.localScale;
    } 
    //variables
    public Vector3 position;
    public Vector3 localScale;
    public Quaternion rotation;
}
