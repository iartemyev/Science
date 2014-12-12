using System;
using System.Collections.Generic;

// "GACUC" (Greedy Agglomerative Category Utility Clustering) algorithm demo.
// Demo of clustering using Category Utility -- a very clever measure of the goodness of a clustering of categorical data.
// Category Utility is the difference between the expected number of attribute values you would guess correctly without any clustering
// and the expected number you'd guess with a particular clustering. But the details are a bit tricky.
// The clustering algorithm starts by seeding one data tuple into each cluster. Then for each remaining data tuple,
// the algorithm finds the cluster which, if the current tuple were placed there, yields the best (largest) category utility
// and then places the tuple in that cluster. This is a greedy algorithm. The process is repeated and the best clustering result is returned.
// Coded using static methods, with normal error-checking removed, for clarity.

namespace Science.Tools.Clustering.KMeans
{
  class ClusteringCategoricalProgram
  {
    static Random random = null;  // used when selecting candidate seed tuples

    //static void Main(string[] args)
    //{
    //  try
    //  {
    //    random = new Random(2);
    //    Console.WriteLine("\nBegin clustering using category utility demo\n");

    //    string[] attNames = new string[] { "Color", "Length", "Rigid" };

    //    string[][] attValues = new string[attNames.Length][]; // 3 attributes = color, length, rigid
    //    attValues[0] = new string[] { "Red", "Blue", "Green", "Yellow" }; // Color
    //    attValues[1] = new string[] { "Short", "Medium", "Long" }; // Length
    //    attValues[2] = new string[] { "False", "True" }; // Rigid

    //    string[][] tuples = new string[5][];
    //    tuples[0] = new string[] { "Red", "Short", "True" };
    //    tuples[1] = new string[] { "Red", "Long", "False" };
    //    tuples[2] = new string[] { "Blue", "Medium", "True" };
    //    tuples[3] = new string[] { "Green", "Medium", "True" };
    //    tuples[4] = new string[] { "Green", "Medium", "False" };
        
    //    Console.WriteLine("Tuples in raw (string) form:\n");
    //    Console.WriteLine("Color    Length   Rigid");
    //    Console.WriteLine("-------------------------");
    //    DisplayMatrix(tuples);

    //    Console.WriteLine("\nConverting tuples from string to int");
    //    int[][] tuplesAsInt = TuplesToInts(tuples, attValues);

    //    Console.WriteLine("\nTuples in integer form:\n");
    //    DisplayMatrix(tuplesAsInt);

    //    int numClusters = 2;
    //    int numSeedTrials = 10;  // times to iterate to get good seed indexes (dissimilar tuples)
    //    Console.WriteLine("\nSetting numClusters to " + numClusters);

    //    Console.WriteLine("Initializing clustering result array");
    //    int[] clustering = InitClustering(tuplesAsInt);

    //    Console.WriteLine("Initializing value counts, value sums, and cluster counts\n");
    //    int[][][] valueCounts = InitValueCounts(tuplesAsInt, attValues, numClusters); // inits with 0 in all cells
    //    int[][] valueSums = InitValueSums(tuplesAsInt, attValues);
    //    int[] clusterCounts = new int[numClusters];  // implicitly initialized to 0

    //    Console.WriteLine("\nBeginning clustering routine");
    //    Cluster(tuplesAsInt, attValues, clustering, valueCounts, valueSums, clusterCounts, numSeedTrials);
    //    Console.WriteLine("Clustering complete");
    //    double cu = CategoryUtility(valueCounts, valueSums, clusterCounts);
    //    Console.WriteLine("\nCategory Utility of clustering = " + cu.ToString("F4"));
    //    Console.WriteLine("Preliminary clustering in internal form:\n");
    //    DisplayVector(clustering);

    //    Console.WriteLine("\nAttempting to refine clustering");
    //    Refine(20, tuplesAsInt, clustering, valueCounts, valueSums, clusterCounts);
    //    Console.WriteLine("Refining complete");
    //    Console.WriteLine("\nFinal clustering in internal form:\n");
    //    DisplayVector(clustering);

    //    Console.WriteLine("\nFinal clustering in string form:\n");
    //    DisplayClustering(numClusters, clustering, tuples);

    //    cu = CategoryUtility(valueCounts, valueSums, clusterCounts);
    //    Console.WriteLine("\nCategory Utility of final clustering = " + cu.ToString("F4"));
      
    //    Console.WriteLine("\nEnd demo\n");
    //    Console.ReadLine();
    //  }
    //  catch (Exception ex)
    //  {
    //    Console.WriteLine(ex.Message);
    //    Console.ReadLine();
    //  }
    //} // Main

    // ------------------------------------------------------------------------------------------------------------------

    static void Cluster(int[][] tuplesAsInt, string[][] attValues, int[] clustering, int[][][] valueCounts, int[][] valueSums, int[] clusterCounts, int numSeedTrials)
    {
      // seed clustering[] with numClusters good tuples (tuples that are dissimilar)
      // for each remaining tuple, compute the CUs for each cluster if the tuple were to be added to that cluster
      // determine the cluster that generates the greatest (best) CU and add curr tuple to that best cluster

      int numClusters = clusterCounts.Length;
      int[] goodIndexes = GetGoodIndexes(tuplesAsInt, attValues, numSeedTrials, numClusters);

      Console.Write("Seeding clusters with tuples: ");
      for (int k = 0; k < goodIndexes.Length; ++k)
        Console.Write(goodIndexes[k] + " ");
      Console.WriteLine("");

      for (int k = 0; k < numClusters; ++k)
      {
        Assign(goodIndexes[k], tuplesAsInt, k, clustering, valueCounts, valueSums, clusterCounts);
      }

      double currCU = CategoryUtility(valueCounts, valueSums, clusterCounts);

      for (int t = 0; t < tuplesAsInt.Length; ++t)  // walk thru each tuple
      {
        if (clustering[t] != -1) continue;  // tuple has already been clustered

        double[] candidates = new double[numClusters];  // candidate CU values

        for (int k = 0; k < numClusters; ++k)
        {
          Assign(t, tuplesAsInt, k, clustering, valueCounts, valueSums, clusterCounts);
          candidates[k] = CategoryUtility(valueCounts, valueSums, clusterCounts);
          Unassign(t, tuplesAsInt, k, clustering, valueCounts, valueSums, clusterCounts);
        }

        int bestK = IndexOfBestCU(candidates);  // the index is a cluster ID
        Assign(t, tuplesAsInt, bestK, clustering, valueCounts, valueSums, clusterCounts);
      } // each tuple
    }

    static int IndexOfBestCU(double[] cus)
    {
      double bestCU = 0.0;
      int indexOfBestCU = 0;
      for (int k = 0; k < cus.Length; ++k)
      {
        if (cus[k] > bestCU)
        {
          bestCU = cus[k];
          indexOfBestCU = k;
        }
      }
      return indexOfBestCU;
    }

    static int[] GetGoodIndexes(int[][] tuplesAsInt, string[][] attValues, int numSeedTrials, int numClusters)
    {
      // indexes of tuples that are dissimilar
      // repeatedly (numSeedTrials times)  get random indexes, create a mini-tuple and mini-clustering  with one tuple in each cluster
      // return indexes that have the greatest (best) CU
      double bestCU = 0.0;
      int[] bestIndexes = new int[numClusters];

      for (int trial = 0; trial < numSeedTrials; ++trial)
      {

        int[][] miniTuples = new int[numClusters][]; // one mini tuple for each cluster
        int numCols = tuplesAsInt[0].Length;         // number attributes
        for (int i = 0; i < miniTuples.Length; ++i)
          miniTuples[i] = new int[numCols];

        int[][][] miniValueCounts = InitValueCounts(tuplesAsInt, attValues, numClusters);
        int[][] miniValueSums = InitValueSums(tuplesAsInt, attValues);
        int[] miniClusterCounts = new int[numClusters];

        int[] miniClustering = InitClustering(tuplesAsInt);

        int[] randomIndexes = RandomDistinctIndexes(numClusters, tuplesAsInt.Length);
        for (int i = 0; i < miniTuples.Length; ++i)
        {
          for (int j = 0; j < miniTuples[i].Length; ++j)
          {
            miniTuples[i][j] = tuplesAsInt[randomIndexes[i]][j];
          }
        }

        for (int k = 0; k < randomIndexes.Length; ++k)
          Assign(k, miniTuples, k, miniClustering, miniValueCounts, miniValueSums, miniClusterCounts);  // assign minTuple at [k] to cluster k

        double cu = CategoryUtility(miniValueCounts, miniValueSums, miniClusterCounts);

        if (cu > bestCU)
        {
          bestCU = cu;
          Array.Copy(randomIndexes, bestIndexes, randomIndexes.Length);
        }
      }
      return bestIndexes;
    }

    static int[] RandomDistinctIndexes(int n, int maxIndex)
    {
      // helper for GetGoodIndexes
      // get n distinct indexes between [0,maxIndex) // [inclusive,exclusive)
      // assumes a global (class-scope) random object exists
      Dictionary<int, bool> d = new Dictionary<int, bool>();  // key is an index, bool is true if already used (dummy)

      int[] result = new int[n];
      int ct = 0;
      int sanityCount = 0;  // to prevent an infinite loop
      while (ct < n && sanityCount < 10000)
      {
        ++sanityCount;
        int idx = random.Next(0, maxIndex);
        if (d.ContainsKey(idx) == false)
        {
          result[ct] = idx;
          d[idx] = true;  // dummy
          ++ct;
        }
      }
      return result;
    }

    // ------------------------------------------------------------------------------------------------------------------

    static void Assign(int tupleIndex, int[][] tuplesAsInt, int cluster, int[] clustering, int[][][] valueCounts, int[][] valueSums, int[] clusterCounts)
    {
      // assign tuple at tupleIndex to clustering[] cluster, and update valueCounts[][][], valueSums[][], clusterCounts[]
      clustering[tupleIndex] = cluster;  // assign

      for (int i = 0; i < valueCounts.Length; ++i)  // update valueCounts and valueSums. i is attribute
      {
        int v = tuplesAsInt[tupleIndex][i]; // att value
        ++valueCounts[i][v][cluster];       // ex: bump count of att color, value red, for cluster 2 
        ++valueSums[i][v];                  // ex: bump sum of counts for att color, value red (all clusters)
      }
      ++clusterCounts[cluster];  // update clusterCounts
    }

    static void Unassign(int tupleIndex, int[][] tuplesAsInt, int cluster, int[] clustering, int[][][] valueCounts, int[][] valueSums, int[] clusterCounts)
    {
      clustering[tupleIndex] = -1;  // unassign
      for (int i = 0; i < valueCounts.Length; ++i)  // update
      {
        int v = tuplesAsInt[tupleIndex][i];
        --valueCounts[i][v][cluster];
        --valueSums[i][v]; 
      }
      --clusterCounts[cluster];  // update clusterCounts
    }

    // ------------------------------------------------------------------------------------------------------------------

    static double CategoryUtility(int[][][] valueCounts, int[][] valueSums, int[] clusterCounts)
    {
      // compute probability of each cluster
      int numTuplesAssigned = 0;
      for (int k = 0; k < clusterCounts.Length; ++k)
        numTuplesAssigned += clusterCounts[k];

      int numClusters = clusterCounts.Length;
      double[] clusterProbs = new double[numClusters];   // P(Ck)
      for (int k = 0; k < numClusters; ++k)
        clusterProbs[k] = (clusterCounts[k] * 1.0) / numTuplesAssigned;

      // compute the unconditional probability term:
      // sum of squared probabilities of att values across all clusters
      double unconditional = 0.0;
      for (int i = 0; i < valueSums.Length; ++i)
      {
        for (int j = 0; j < valueSums[i].Length; ++j)
        {
          double p = (valueSums[i][j] * 1.0) / numTuplesAssigned;
          unconditional += (p * p);
        }
      }

      // compute the conditional probabilitiews for each cluster
      double[] conditionals = new double[numClusters];
      for (int k = 0; k < numClusters; ++k)
      {

        for (int i = 0; i < valueCounts.Length; ++i)
        {
          for (int j = 0; j < valueCounts[i].Length; ++j)
          {
            double p = (valueCounts[i][j][k] * 1.0) / clusterCounts[k];
            conditionals[k] += (p * p);
          }
        }
      }
   
      // we have P(Ck), EE P(Ai=Vij|Ck)^2, EE P(Ai=Vij)^2 so we can compute CU easily
      double summation = 0.0;
      for (int k = 0; k < numClusters; ++k)
        summation += clusterProbs[k] * (conditionals[k] - unconditional);  // E P(Ck) * [ EE P(Ai=Vij|Ck)^2 - EE P(Ai=Vij)^2 ] / n

      return summation / numClusters;
    }

    // ------------------------------------------------------------------------------------------------------------------

    static void Refine(int numRefineTrials, int[][] tuplesAsInt, int[] clustering, int[][][] valueCounts, int[][] valueSums, int[] clusterCounts)
    {
      // attempt to refine a clustering
      // repeatedly move a random tuple to a new cluster and see if the CU improves 
      for (int n = 0; n < numRefineTrials; ++n)
      {
        int randomTupleIndex = GetTupleIndex(tuplesAsInt, clustering, clusterCounts);  // pick a tuple at random

        int clusterOfTuple = clustering[randomTupleIndex];  // what cluster is the random tuple assigned to?
        int differentCluster = GetDifferentCluster(clusterOfTuple, clusterCounts);  // pick a different cluster to move the random tuple to

        double currCU = CategoryUtility(valueCounts, valueSums, clusterCounts);  // current CU
        Unassign(randomTupleIndex, tuplesAsInt, clusterOfTuple, clustering, valueCounts, valueSums, clusterCounts); // remove from curr cluster
        Assign(randomTupleIndex, tuplesAsInt, differentCluster, clustering, valueCounts, valueSums, clusterCounts); // add to different cluster

        double newCU = CategoryUtility(valueCounts, valueSums, clusterCounts); // what is the new CU after the cluster assignment change?
 
        if (newCU > currCU) // improvement
        {
          ; // leave the cluster assignment change in effect
          //Console.WriteLine("improved. switching tuple " + randomTupleIndex + " from cluster " + clusterOfTuple + " to " + differentCluster);
          //Console.ReadLine();
        }
        else // no improvement so undo the clustering change
        {
          Unassign(randomTupleIndex, tuplesAsInt, differentCluster, clustering, valueCounts, valueSums, clusterCounts);
          Assign(randomTupleIndex, tuplesAsInt, clusterOfTuple, clustering, valueCounts, valueSums, clusterCounts);
        }

      }
    }

    static int GetTupleIndex(int[][] tuplesAsInt, int[] clustering, int[] clusterCounts)
    {
      // helper for Refine
      // get the index of a random tuple, where the tuple is not the only member of a cluster
      // or, in other words, index of a tuple that is part of a cluster with 2 or more tuples.
      int sanityCount = 0;
      while (sanityCount < 10000)
      {
        ++sanityCount;
        int ri = random.Next(0, tuplesAsInt.Length); // a candidate index of a tuple
        int c = clustering[ri];                      // cluster that the tuple is assigned to
        if (clusterCounts[c] > 1)                    // the cluster has 2 or more tuples assigned
          return ri;
      }
      return -1; //error
    }

    static int GetDifferentCluster(int cluster, int[] clusterCounts)
    {
      // get a cluster that is different from cluster
      int sanityCount = 0;
      while (sanityCount < 10000)
      {
        ++sanityCount;
        int c = random.Next(0, clusterCounts.Length);
        if (c != cluster)
          return c;
      }
      return -1; // error
    }


    // ------------------------------------------------------------------------------------------------------------------


    // ------------------------------------------------------------------------------------------------------------------
    static int[] InitClustering(int[][] tuplesAsInt)
    {
      int[] clustering = new int[tuplesAsInt.Length];
      for (int i = 0; i < clustering.Length; ++i)
        clustering[i] = -1;
      return clustering;
    }

    static int[][][] InitValueCounts(int[][] tuplesAsInt, string[][] attValues, int numClusters)
    {
      // allocate joint counts and set all cells to 1 for Laplacian smoothing (a joint count of 0 is bad)
      // [attribute][attValue][cluster]. ex: [1][0][2] holds the count of income(1) equal to lown (0) AND cluster c(2)

      int[][][] result = new int[attValues.Length][][]; // allocate
      for (int i = 0; i < result.Length; ++i)
      {
        int numCells = attValues[i].Length;
        result[i] = new int[numCells][];
      }

      for (int i = 0; i < result.Length; ++i)
      {
        for (int j = 0; j < result[i].Length; ++j)
        {
          result[i][j] = new int[numClusters];
        }
      }

      for (int i = 0; i < result.Length; ++i)         // assign 0 to each cell
        for (int j = 0; j < result[i].Length; ++j)
          for (int k = 0; k < result[i][j].Length; ++k)
            result[i][j][k] = 0;  // not really necessary in C# but often required in other languages

      return result;

    } // InitValueCounts

    static int[][] InitValueSums(int[][] tuplesAsInt, string[][] attValues)
    {
      // sums for each attribute value across all clusters
      // could compute from ValueCounts but storing will make faster CU computation
      // ex: [0][1] = sum of counts of attribute 0 (color) value 1 (blue) for all clustered tuples
      int[][] result = new int[attValues.Length][]; // allocate
      for (int i = 0; i < result.Length; ++i)
      {
        int numCells = attValues[i].Length;
        result[i] = new int[numCells];
      }
      return result;
    }

    static void DisplayValueCounts(int[][][] valueCounts)
    {
      for (int i = 0; i < valueCounts.Length; ++i)
      {
        for (int j = 0; j < valueCounts[i].Length; ++j)
        {
          for (int k = 0; k < valueCounts[i][j].Length; ++k)
          {
            Console.Write(valueCounts[i][j][k] + " ");
          }
          Console.WriteLine("");
        }
        Console.WriteLine("");
      }
    }

    static void DisplayValueSums(int[][] valueSums)
    {
      for (int i = 0; i < valueSums.Length; ++i)
      {
        for (int j = 0; j < valueSums[i].Length; ++j)
        {
          Console.Write(valueSums[i][j] + " ");
        }
        Console.WriteLine("");
      }
    }

    // ------------------------------------------------------------------------------------------------------------------


    static int[][] TuplesToInts(string[][] tuples, string[][] attValues) // converts a matrix of tuples in string form to int form
    {
      // return an array of arrays of ints
      // create an array of lookup table thing. [0] = attribute (e.g., [0] = color), key = "green", then value = 2
      Dictionary<string, int>[] lookup = new Dictionary<string, int>[attValues.Length];  // one dictionary for each attribute
      for (int i = 0; i < attValues.Length; ++i)  // each attribute
      {
        lookup[i] = new Dictionary<string, int>();
        for (int j = 0; j < attValues[i].Length; ++j) // each value of curr attribute
          lookup[i].Add(attValues[i][j], j); //
      }
      // scan tuples and convert using the lookup
      int numRows = tuples.Length;
      int numCols = attValues.Length;
      int[][] result = new int[numRows][]; // allocate
      for (int i = 0; i < numRows; ++i)
        result[i] = new int[numCols];

      for (int i = 0; i < numRows; ++i) // each row/tuple
      {
        for (int j = 0; j < numCols; ++j) // each col/attribute
        {
          string v = tuples[i][j]; // eg, "red"
          int attAsInt = lookup[j][v]; // then j is the tuple column which is also the attribute
          result[i][j] = attAsInt;
        }
      }
      return result;
    } // TuplesToInts

    static void DisplayClustering(int numClusters, int[] clustering, string[][] tuples)
    {
      Console.WriteLine("-------------------------");
      for (int k = 0; k < numClusters; ++k) // display by cluster
      {
        for (int i = 0; i < tuples.Length; ++i) // each tuple
        {
          if (clustering[i] == k) // curr tuple i belongs to curr cluster k. automatically filters out when k = -1 (no assigned cluster)
          {
            for (int j = 0; j < tuples[i].Length; ++j)
            {
              Console.Write(tuples[i][j].ToString().PadRight(8) + " ");
            }
            Console.WriteLine("");
          }
        }
        Console.WriteLine("-------------------------");
      }
    }

    static void DisplayVector(int[] vector) // for clustering
    {
      for (int i = 0; i < vector.Length; ++i)
        Console.Write(vector[i] + " ");
      Console.WriteLine("");
    }

    static void DisplayVector(double[] vector)
    {
      for (int i = 0; i < vector.Length; ++i)
        Console.Write(vector[i].ToString("F4") + " ");
      Console.WriteLine("");
    }

    static void DisplayMatrix(string[][] matrix) // for tuples
    {
      for (int i = 0; i < matrix.Length; ++i)
      {
        Console.Write("[" + i + "] ");
        for (int j = 0; j < matrix[i].Length; ++j)
          Console.Write(matrix[i][j].ToString().PadRight(8) + " ");
        Console.WriteLine("");
      }
    }

    static void DisplayMatrix(int[][] matrix) // for tuplesAsInt
    {
      for (int i = 0; i < matrix.Length; ++i)
      {
        Console.Write("[" + i + "] ");
        for (int j = 0; j < matrix[i].Length; ++j)
          Console.Write(matrix[i][j] + " ");
        Console.WriteLine("");
      }
    }

  } // class Program
} // ns
