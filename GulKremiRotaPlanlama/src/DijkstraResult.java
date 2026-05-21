import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class DijkstraResult {
    private final int startNode;
    private final Map<Integer, Double> distances;
    private final Map<Integer, Integer> previousNodes;

    public DijkstraResult(int startNode, Map<Integer, Double> distances, Map<Integer, Integer> previousNodes) {
        this.startNode = startNode;
        this.distances = new HashMap<>(distances);
        this.previousNodes = new HashMap<>(previousNodes);
    }

    public double distanceTo(int targetNode) {
        return distances.getOrDefault(targetNode, Double.POSITIVE_INFINITY);
    }

    public List<Integer> pathTo(int targetNode) {
        if (!distances.containsKey(targetNode) || Double.isInfinite(distanceTo(targetNode))) {
            return Collections.emptyList();
        }

        List<Integer> path = new ArrayList<>();
        Integer current = targetNode;

        while (current != null) {
            path.add(current);
            if (current == startNode) {
                break;
            }
            current = previousNodes.get(current);
        }

        Collections.reverse(path);
        return path;
    }
}
