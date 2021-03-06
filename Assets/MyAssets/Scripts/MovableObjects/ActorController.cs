﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ActorController {
    void Move(Vector3 dir, float speed);
    void LookAt(Vector3 dir);
    void Init(World world);
    CustomOBB GetOBB();
    Vector3 GetMinExtent();
    Vector3 GetMaxExtent();
    Transform GetActorTransform();
}
