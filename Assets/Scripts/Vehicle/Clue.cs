using System;
using UnityEngine;
[Serializable]
public class Clue
{
    public string clueText;
    public bool isAcitve = false;
    public bool isCollected = false;
    public Guid ClueGuid=Guid.NewGuid();
}

[Serializable]
public class ClueDto
{
    public string clueText;
    public bool isActive = false;
    public bool isCollected;
    public string clueGuid;
}