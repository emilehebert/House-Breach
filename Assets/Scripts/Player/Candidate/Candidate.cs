using System.Collections.Generic;
using UnityEngine;

// Right-click in the Project window -> Create -> Candidates -> Candidate
[CreateAssetMenu(menuName = "Candidates/Candidate")]
public class Candidate : ScriptableObject
{
    public string candidateName;
    public float height;
    public float weight;
    public float speed;
}