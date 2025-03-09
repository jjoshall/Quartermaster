using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class IProjectile : MonoBehaviour
{
    public GameObject sourcePlayer { get; set; } = null;

    protected virtual float _expireTimer { get; set; } = 0f;

    protected bool _projectileCollided = false;

    protected abstract void Start();

    protected abstract void OnCollisionEnter(Collision collision);

    protected abstract void Update();

}
