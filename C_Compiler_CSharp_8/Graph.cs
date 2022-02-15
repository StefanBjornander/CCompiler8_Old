using System.Collections.Generic;

namespace CCompiler {
  public class Graph<VertexType> {
    private ISet<VertexType> m_vertexSet;
    private ISet<Pair<VertexType,VertexType>> m_edgeSet;

    public Graph() {
      m_vertexSet = new HashSet<VertexType>();
      m_edgeSet = new HashSet<Pair<VertexType,VertexType>>();
    }
  
    public Graph(ISet<VertexType> vertexSet) {
      m_vertexSet = vertexSet;
      m_edgeSet = new HashSet<Pair<VertexType,VertexType>>();
    }

    public Graph(ISet<VertexType> vertexSet,
                 ISet<Pair<VertexType,VertexType>> edgeSet) {
      m_vertexSet = vertexSet;
      m_edgeSet = edgeSet;
    }

    public ISet<VertexType> VertexSet {
      get { return m_vertexSet; }
    }
  
    public ISet<Pair<VertexType,VertexType>> EdgeSet {
      get { return m_edgeSet; }
    }
//The neighbourSet method goes through all edges and add each found neighbor to the vertex.
    public ISet<VertexType> GetNeighbourSet(VertexType vertex) {
      ISet<VertexType> neighbourSet = new HashSet<VertexType>();
    
      foreach (Pair<VertexType,VertexType> edge in m_edgeSet) {
        if (edge.First.Equals(vertex)) {
          neighbourSet.Add(edge.Second);
        }
      
        if (edge.Second.Equals(vertex)) {
          neighbourSet.Add(edge.First);
        }      
      }
    
      return neighbourSet;
    }
//E.3.2. 	Addition and Removal of Vertices and Edges
    public void AddVertex(VertexType vertex) {
      m_vertexSet.Add(vertex);
    }

    public void EraseVertex(VertexType vertex) {
      ISet<Pair<VertexType,VertexType>> edgeSetCopy =
        new HashSet<Pair<VertexType,VertexType>>(m_edgeSet);

      foreach (Pair<VertexType,VertexType> edge in edgeSetCopy) {
        if ((vertex.Equals(edge.First)) || (vertex.Equals(edge.Second))) {
          m_edgeSet.Remove(edge);
        }
      }

      m_vertexSet.Remove(vertex);
    }

    public void AddEdge(VertexType vertex1, VertexType vertex2) {
      Pair<VertexType,VertexType> edge =
        new Pair<VertexType,VertexType>(vertex1, vertex2);
      m_edgeSet.Add(edge);
    }

    public void EraseEdge(VertexType vertex1, VertexType vertex2) {
      Pair<VertexType,VertexType> edge =
        new Pair<VertexType,VertexType>(vertex1, vertex2);
      m_edgeSet.Remove(edge);
    }
//E.3.3. 	Graph Partition
//The method partitionate divides the graph into free subgraphs; that is, subgraphs which vertices have no neighbors in any of the other free subgraphs. First, we go through the vertices and perform a deep search to find all vertices reachable from the vertex. Then we generate a subgraph for each such vertex set.
    public ISet<Graph<VertexType>> Split() {
      ISet<ISet<VertexType>> subgraphSet = new HashSet<ISet<VertexType>>();

      foreach (VertexType vertex in m_vertexSet) {
        ISet<VertexType> vertexSet = new HashSet<VertexType>();
        DeepSearch(vertex, vertexSet);
        subgraphSet.Add(vertexSet);
      }

      ISet<Graph<VertexType>> graphSet = new HashSet<Graph<VertexType>>();
      foreach (ISet<VertexType> vertexSet in subgraphSet) {
        Graph<VertexType> subGraph = InducedSubGraph(vertexSet);
        graphSet.Add(subGraph);
      }

      return graphSet;
    }
//The DeepFirstSearch method search recursively through the graph in order to find all vertices reachable from the given vertex. To avoid cyclic search, the search is terminated if the vertex is already a member of the result set.
    private void DeepSearch(VertexType vertex, ISet<VertexType> resultSet) {
      if (!resultSet.Contains(vertex)) {
        resultSet.Add(vertex);
        ISet<VertexType> neighbourSet = GetNeighbourSet(vertex);
        
        foreach (VertexType neighbour in neighbourSet) {
          DeepSearch(neighbour, resultSet);
        }
      }
    }
//The InducedSubGraph method goes through the edge set and add all edges which both end vertices are members of the vertex set.
    private Graph<VertexType> InducedSubGraph(ISet<VertexType> vertexSet) {
      ISet<Pair<VertexType,VertexType>> resultEdgeSet =
        new HashSet<Pair<VertexType,VertexType>>();
   
      foreach (Pair<VertexType,VertexType> edge in m_edgeSet) {
        if (vertexSet.Contains(edge.First) &&
            vertexSet.Contains(edge.Second)) {
          resultEdgeSet.Add(edge);
        }
      }
   
      return (new Graph<VertexType>(vertexSet, resultEdgeSet));
    } 
  }
}
