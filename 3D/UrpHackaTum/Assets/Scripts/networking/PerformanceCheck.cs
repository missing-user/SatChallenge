using System;
using System.Collections;
using UnityEngine;
public class PerformanceCheck : MonoBehaviour
{
    public void Start()     {
        StartCoroutine(Wait(3));
    }

    IEnumerator Wait(float duration)
    {
        //This is a coroutine
        Debug.Log("Start Wait() function. The time is: " + Time.time);
        Debug.Log("Float duration = " + duration);
        yield return new WaitForSeconds(duration);   //Wait

        for (int g = 0; g < 10; g++)
        {
            var watch = new System.Diagnostics.Stopwatch();
            var RS = new RegionSelector(2048, 2048);

            watch.Start();
            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < 5* 2048; i++)
                {
                    RS.AddEdge((i * 2) % 2048, (3 * i) % 1974);
                }
                RS.hopcroftKarp();
                for (int i = 0; i < 5 * 2048; i++)
                {
                    RS.RemoveEdge((i * 2) % 2048, (3 * i) % 1974);
                }
            }
            watch.Stop();
            Debug.Log($"Execution Time: {watch.ElapsedMilliseconds} ms");
            yield return 0;   //Wait
        }
    }
}
