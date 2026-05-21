import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;

public class ShortestPathTable {
    private final Graph graph;
    private final Map<Integer, DijkstraResult> results = new HashMap<>();

    public ShortestPathTable(Graph graph, Set<Integer> sourceNodes) {
        this.graph = graph;
        for (int sourceNode : sourceNodes) {
            results.put(sourceNode, graph.shortestPathsFrom(sourceNode));
        }
    }

    public double distance(int from, int to) {
        DijkstraResult result = results.computeIfAbsent(from, graph::shortestPathsFrom);
        return result.distanceTo(to);
    }

    public List<Integer> path(int from, int to) {
        DijkstraResult result = results.computeIfAbsent(from, graph::shortestPathsFrom);
        return result.pathTo(to);
    }
}
