using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PizzaChecker : MonoBehaviour
{
    public int pizzaPieces = 8;
    public Dictionary<int, GameObject> indexedSatellites = new Dictionary<int, GameObject>();
    public Dictionary<int, int> indexedRegions = new Dictionary<int, int>();
    public Text status;
    private bool hasBeenFailiure=false;

    private float theta_earth = 45f; //used to be 42f;

    RegionSelector RS = new RegionSelector(4, 4);
    public bool IsInSatelliteCone(Matrix4x4 pizzaMatrix, Vector3 satellite)
    {
        // cone angle from the satellite
        float b_max = (90 + Constants.LOWER_PLANE_ANGLE) * Mathf.Deg2Rad;
        float r = satellite.magnitude;
        float theta_max = -(Mathf.Asin(Constants.R_earth * 1e-6f * Mathf.Sin(b_max) / r) + b_max - Mathf.PI);


        // is the satellite on the correct side of earth?
        Vector3 surfaceNormal = pizzaMatrix * Vector3.up;
        if (Vector3.Dot(surfaceNormal, satellite) < 0)
        {
            return false;
        }

        //are the vertecies in range?
        foreach (Vector3 vtx in pizzaToVertecies(pizzaMatrix, null))
        {
            Vector3 satToVertex = vtx - satellite;
            float dotp = Vector3.Dot(Vector3.Normalize(vtx), -Vector3.Normalize(satToVertex));

#if DEBUG_MATH
    Debug.Log(Mathf.Acos(dotp) * Mathf.Rad2Deg + " should be less than " + theta_max * Mathf.Rad2Deg);
    Debug.DrawRay(satellite, Vector3.Normalize(-satellite), Color.red);
    Debug.DrawRay(satellite, Vector3.Normalize(satToVertex));
#endif

            if (float.IsNaN(theta_max) || Mathf.Acos(dotp) > theta_max)
            {
                return false;
            }
#if DEBUG_MATH
    Debug.Log("found vertex in cone");
    Debug.DrawRay(Vector3.zero, vtx);
#endif
        }
        return true;
    }

    Vector3[] pizzaToVertecies(Matrix4x4 ltow, Color? color)
    {
        // calculating the pizza vertecies
        float theta = theta_earth * Mathf.PI / 180f; // the elevation angle where our pizza pieces start
        float phi = 2f * Mathf.PI / ((float)pizzaPieces);

        Vector3 pole = ltow * Vector3.up;
        Vector3 east = ltow * new Vector3(
            Mathf.Sin(theta) * Mathf.Cos(-Mathf.PI / 2),
            Mathf.Cos(theta),
            Mathf.Sin(theta) * Mathf.Sin(-Mathf.PI / 2));
        Vector3 west = ltow * new Vector3(
            Mathf.Sin(theta) * Mathf.Cos(phi - Mathf.PI / 2),
            Mathf.Cos(theta),
            Mathf.Sin(theta) * Mathf.Sin(phi - Mathf.PI / 2));

        if (color.HasValue)
        {
            Debug.DrawLine(pole, east, color.Value);
            Debug.DrawLine(east, west, color.Value);
            Debug.DrawLine(west, pole, color.Value);
        }

        //are the vertecies in range?
        return new Vector3[] { pole, east, west };
    }

    private Color iToColor(int ind)
    {
        return Color.HSVToRGB((ind % pizzaPieces) / (float)pizzaPieces, 1, 1);
    }

    private Matrix4x4 PizzaMatrix(int ind)
    {
        Matrix4x4 pieceMatrix = transform.localToWorldMatrix;
        pieceMatrix *= Matrix4x4.Rotate(Quaternion.Euler(0, 360f / pizzaPieces * ind, 0));
        if (ind > pizzaPieces)
            pieceMatrix *= Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180));
        return pieceMatrix;
    }


    void reindexModel()
    {
        //reindex the regions (currently just the pizza slices, times 2 because of the two poles)
        indexedRegions.Clear();
        for (int i = 0; i < 2 * pizzaPieces; i++)
        {
            indexedRegions.Add(i+1, i);
        }

        //reindex the satellite game objects
        int j = 1;
        indexedSatellites.Clear();
        foreach (var satObj in GameObject.FindGameObjectsWithTag("Satellite"))
        {
            indexedSatellites.Add(j, satObj);
            j++;
        }

        if (indexedSatellites.Count != RS?.satCount || indexedRegions.Count != RS?.regCount)
        {
            Debug.Log($"indexed Sats: {indexedSatellites.Count} ({RS?.satCount} in matrix) indexed Regions: {indexedRegions.Count} ({RS?.regCount} in matrix)");
            RS = new RegionSelector(indexedSatellites.Count, indexedRegions.Count);
            hasBeenFailiure = false;
        }

    }


    private void Update()
    {
        reindexModel();
        RS.Clear();
        

        foreach (var satTuple in indexedSatellites)
        {
            foreach (var regionTuple in indexedRegions)
            {
                if (IsInSatelliteCone(PizzaMatrix(regionTuple.Key), satTuple.Value.transform.position))
                {
                    RS.AddEdge(satTuple.Key, regionTuple.Key);
                }
            }
        }

        if (status != null)
        {
            int validConnections = RS.hopcroftKarp();
            hasBeenFailiure = hasBeenFailiure | validConnections != indexedRegions.Count;
            status.text = $"{validConnections} of {indexedRegions.Count} regions are covered \n {(hasBeenFailiure?"Coverage issues detected":"")}";
        }

        foreach (var regionTuple in indexedRegions)
        {
            int sat = RS.getSatelliteForRegion(regionTuple.Key);
            var tmp = pizzaToVertecies(PizzaMatrix(regionTuple.Key), iToColor(regionTuple.Key));
            Vector3 avg = (tmp[0] + tmp[1] + tmp[2]) / 3f;

            if (indexedSatellites.ContainsKey(sat))
            {
                Debug.DrawLine(avg, indexedSatellites[sat].transform.position, iToColor(regionTuple.Key));
                indexedSatellites[sat].GetComponentInChildren<Renderer>().material.color = iToColor(sat);

            }
            else
            {
                //TODO: find out why this is happening
                Debug.LogWarning($"couldn't find key {sat} that was used during matching");
            }
        }
    }
}
