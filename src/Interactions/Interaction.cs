public interface Interaction { // <T> ?
  void Init();
  void Frame();
  // void Drop();
	bool Active { get; set; }
}