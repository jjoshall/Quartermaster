using UnityEngine;

public abstract class IShaderAnimator : MonoBehaviour
{
    public Renderer renderer;
    public bool loop = false;

    public abstract void Animate();

}
