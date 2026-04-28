namespace ASLTv1.Models
{
    public enum UndoActionType { AddBox, RemoveBox, ModifyBox, RemoveWaypointWithBoxes }

    public class UndoAction
    {
        public UndoActionType Type { get; set; }
        public BoundingBox Box { get; set; }
        public Rectangle OriginalRectangle { get; set; }
        public string OriginalLabel { get; set; }
        public int OriginalObjectId { get; set; }

        // RemoveWaypointWithBoxes: Waypoint + 그 구간 박스들의 atomic 삭제/복원.
        public List<BoundingBox> AffectedBoxes { get; set; }
        public WaypointMarker AffectedWaypoint { get; set; }
    }

    public enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public class CustomLabel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Id { get; set; }
        public Panel Panel { get; set; }
        public Label Label { get; set; }
    }

    public class BoundingBox
    {
        public int FrameIndex { get; set; }
        public Rectangle Rectangle { get; set; }
        public string Label { get; set; }
        public int PersonId { get; set; }
        public int VehicleId { get; set; }
        public int EventId { get; set; }
        public string Action { get; set; }
        public string VehicleName { get; set; }
        public string EventName { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SubtitleEntry
    {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; }
    }
}
