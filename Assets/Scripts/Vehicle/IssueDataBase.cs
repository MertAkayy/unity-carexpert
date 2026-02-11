using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Issue DataBase", menuName = "Vehicle/Issue DataBase")]
public class IssueDataBase : ScriptableObject, IEnumerable
{
    public List<Issue> issues;
    public IEnumerator<Issue> GetEnumerator()
    {
        return issues.GetEnumerator();
    }
    public Issue GetByName(string name)
    {
        return issues.Find(i => i.FailureName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public List<Issue> GetAll()
    {
        return issues;
    }
    
    public List<Issue> GetByPartType(AffectedPartType partType)
    {
        return issues.FindAll(i => i.AffectedPartType == partType);
    }
    
    public List<Issue> GetAvailableForLevel(int level)
    {
        return issues.FindAll(i => i.AvailableLevel <= level);
    }
    
    public Issue GetByObdCode(string obdCode)
    {
        return issues.Find(i => i.ObdCode == obdCode);
    }
    
    public Issue GetRandom()
    {
        if (issues.Count == 0) return null;
        return issues[Random.Range(0, issues.Count)];
    }
    public Issue GetRandomByWeight()
    {
        int totalWeight = 0;
        foreach (var issue in issues)
            totalWeight += issue.PossibilityWeight;

        int randomValue = Random.Range(0, totalWeight);
        int current = 0;

        foreach (var issue in issues)
        {
            current += issue.PossibilityWeight;
            if (randomValue < current)
                return issue;
        }
        return null;
    }

    // 7. Araç parçasına uygun Issue’lar
    public List<Issue> GetValidForPart(VehiclePart part)
    {
        return issues.FindAll(i => i.IsValidFor(part));
    }
}
