using System.Collections.Generic;

class RegionSelector
{
    /* Hopcroft Karp based Bipartite Graph matching algo from
     * https://en.wikipedia.org/wiki/Hopcroft%E2%80%93Karp_algorithm
     * adapted from a Java Example on https://www.geeksforgeeks.org/hopcroft-karp-algorithm-for-maximum-matching-set-1-introduction/
     */
    public int satCount { get => (int)(sats); }
    public int regCount { get => (int)(regs); }

    ushort sats, regs;
    private List<ushort>[] adjMat;
    ushort[] pairS, pairR, dist;
    public RegionSelector(int s, int r)
    {
        sats = (ushort)s;
        regs = (ushort)r;

        adjMat = new List<ushort>[s + 1];
        for (int i = 0; i < adjMat.Length; i++)
            adjMat[i] = new List<ushort>();
    }
    
    // Returns true if there is an augmenting
    // path, else returns false
    bool bfs()
    {
        LinkedList<ushort> Q = new LinkedList<ushort>();
        // First layer of vertices (set distance as 0)
        for (ushort s = 1; s <= sats; s++)
        {
            if (pairS[s] == 0)
            {
                // s is not matched
                dist[s] = 0;
                Q.AddLast(s);
            }

            // Else set distance as infinite
            // so that this vertex is
            // considered next time
            else
                dist[s] = ushort.MaxValue;
        }

        dist[0] = ushort.MaxValue;

        while (Q.Count != 0)
        {
            ushort s = Q.First.Value;
            Q.RemoveFirst();

            if (dist[s] < dist[0])
            {
                foreach (ushort r in adjMat[s])
                {
                    if (dist[pairR[r]] == ushort.MaxValue)
                    {
                        dist[pairR[r]] = (ushort)(dist[s] + 1);
                        Q.AddLast(pairR[r]);
                    }
                }
            }
        }

        return (dist[0] != ushort.MaxValue);
    }

    // Returns true if there is an augmenting
    // path beginning with free vertex s
    bool dfs(ushort s)
    {
        if (s != 0)
        {
            foreach (ushort r in adjMat[s])
            {
                // Follow the distances set by BFS
                if (dist[pairR[r]] == dist[s] + 1)
                {

                    // If dfs for pair of r also returns
                    // true
                    if (dfs(pairR[r]))
                    {
                        pairR[r] = s;
                        pairS[s] = r;
                        return true;
                    }
                }
            }

            // If there is no augmenting path
            // beginning with s.
            dist[s] = ushort.MaxValue;
            return false;
        }
        return true;
    }

    public int hopcroftKarp()
    {
        // used during matching to store the current partner (region) of each satellite
        pairS = new ushort[sats + 1];
        // used during matching to store the current partner (satellite) of each region
        pairR = new ushort[regs + 1];
        dist = new ushort[sats + 1];


        // Initialize result (can't be more than the max number of satellites (ushort))
        ushort connections = 0;

        while (bfs())
        {
            for (ushort s = 1; s <= sats; s++)
                // If vertex is free and there is
                // an augmenting path from current vertex
                if (pairS[s] == 0)
                    if (dfs(s))
                        connections++;
        }


        return (int)connections;
    }


    public void AddEdge(int s, int r)
    {
        //the Edge represents a possible connection between the satellite with index s and region r
        adjMat[s].Add((ushort)r);
    }

    public bool RemoveEdge(int s, int r)
    {
        return adjMat[s].Remove((ushort)r);
    }

    public void Clear()
    {
        // reset the graph
        for (int s = 0; s < adjMat.Length; s++)
        {
            adjMat[s].Clear(); 
        }
    }

    public int getSatelliteForRegion(int r)
    {
        return (int)pairR[r];
    }
}
