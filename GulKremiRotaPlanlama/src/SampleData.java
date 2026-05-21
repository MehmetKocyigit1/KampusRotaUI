public final class SampleData {
    private SampleData() {
    }

    public static Graph createIspartaGraph() {
        Graph graph = new Graph();

        graph.addPoint(new DeliveryPoint(0, "Depo", 37.7890, 30.5345));
        graph.addPoint(new DeliveryPoint(1, "Kaymakkapi Magazasi", 37.7647, 30.5537));
        graph.addPoint(new DeliveryPoint(2, "Mimar Sinan Subesi", 37.7663, 30.5548));
        graph.addPoint(new DeliveryPoint(3, "IYAS Park Noktasi", 37.7737, 30.5526));
        graph.addPoint(new DeliveryPoint(4, "Otogar Bayisi", 37.7759, 30.5483));
        graph.addPoint(new DeliveryPoint(5, "SDU Kampus Noktasi", 37.8321, 30.5264));
        graph.addPoint(new DeliveryPoint(6, "Cunur Subesi", 37.8135, 30.5322));
        graph.addPoint(new DeliveryPoint(7, "Bahcelievler Noktasi", 37.7625, 30.5448));
        graph.addPoint(new DeliveryPoint(8, "Modernevler Subesi", 37.7712, 30.5618));
        graph.addPoint(new DeliveryPoint(9, "Fatih Mahallesi Noktasi", 37.7824, 30.5643));
        graph.addPoint(new DeliveryPoint(10, "Davraz Noktasi", 37.7526, 30.5758));
        graph.addPoint(new DeliveryPoint(11, "Gulistan Subesi", 37.7543, 30.5615));
        graph.addPoint(new DeliveryPoint(12, "Karaagac Noktasi", 37.7605, 30.5821));
        graph.addPoint(new DeliveryPoint(13, "Sanayi Bayisi", 37.7834, 30.5413));
        graph.addPoint(new DeliveryPoint(14, "Gokcay Noktasi", 37.7490, 30.5401));
        graph.addPoint(new DeliveryPoint(15, "Anadolu Mahallesi Noktasi", 37.7689, 30.5680));
        graph.addPoint(new DeliveryPoint(16, "Vatan Mahallesi Noktasi", 37.7558, 30.5489));
        graph.addPoint(new DeliveryPoint(17, "Cayboyu Noktasi", 37.7707, 30.5368));
        graph.addPoint(new DeliveryPoint(18, "Sermet Subesi", 37.7585, 30.5337));
        graph.addPoint(new DeliveryPoint(19, "Yedisehitler Noktasi", 37.7442, 30.5529));
        graph.addPoint(new DeliveryPoint(20, "Akkent Noktasi", 37.7375, 30.5690));

        addRoad(graph, 0, 13);
        addRoad(graph, 0, 4);
        addRoad(graph, 0, 17);
        addRoad(graph, 1, 2);
        addRoad(graph, 1, 7);
        addRoad(graph, 1, 11);
        addRoad(graph, 1, 15);
        addRoad(graph, 2, 3);
        addRoad(graph, 2, 8);
        addRoad(graph, 2, 15);
        addRoad(graph, 3, 4);
        addRoad(graph, 3, 8);
        addRoad(graph, 3, 17);
        addRoad(graph, 3, 13);
        addRoad(graph, 4, 5);
        addRoad(graph, 4, 6);
        addRoad(graph, 4, 13);
        addRoad(graph, 4, 17);
        addRoad(graph, 5, 6);
        addRoad(graph, 6, 9);
        addRoad(graph, 6, 13);
        addRoad(graph, 7, 16);
        addRoad(graph, 7, 17);
        addRoad(graph, 7, 18);
        addRoad(graph, 8, 9);
        addRoad(graph, 8, 15);
        addRoad(graph, 9, 13);
        addRoad(graph, 10, 11);
        addRoad(graph, 10, 12);
        addRoad(graph, 10, 15);
        addRoad(graph, 10, 20);
        addRoad(graph, 11, 16);
        addRoad(graph, 11, 19);
        addRoad(graph, 12, 15);
        addRoad(graph, 12, 20);
        addRoad(graph, 14, 16);
        addRoad(graph, 14, 18);
        addRoad(graph, 14, 19);
        addRoad(graph, 16, 18);
        addRoad(graph, 16, 19);
        addRoad(graph, 17, 18);
        addRoad(graph, 19, 20);

        return graph;
    }

    private static void addRoad(Graph graph, int firstNode, int secondNode) {
        DeliveryPoint first = graph.getPoint(firstNode);
        DeliveryPoint second = graph.getPoint(secondNode);
        graph.addUndirectedEdge(firstNode, secondNode, DistanceUtil.estimatedRoadDistanceKm(first, second));
    }
}
