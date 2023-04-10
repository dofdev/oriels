public interface dof { // <T> ?
  void Init();
  void Frame();
  // void Drop();
	bool Active { get; set; }
}