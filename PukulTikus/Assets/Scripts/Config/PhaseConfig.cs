using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PhaseConfig", menuName = "WhackAMole/PhaseConfig")]
public class PhaseConfig : ScriptableObject
{
    [Header("Active Holes (grid coords 4x3)")]
    [Tooltip("Koordinat lubang aktif: (x,y) dengan x:0..3, y:0..2. Contoh Fase1: (1,0),(2,0),(1,1),(2,1)")]
    public List<Vector2Int> activeHoles = new();

    [Header("Spawn")]
    [Tooltip("Interval spawn antar mole (detik)")]
    public float spawnInterval = 1.0f;

    [Tooltip("Lifetime mole (detik) - berapa lama nongol menunggu di lubang")]
    public float lifetime = 1.0f;

    [Tooltip("Maksimal mole aktif bersamaan pada fase ini")]
    public int maxConcurrent = 1;

    [Tooltip("Kuota total mole fase ini (abaikan pada fase infinite, isi -1)")]
    public int quota = 10;

    [Header("Spawn Weights (0..1, akan dinormalisasi)")]
    [Range(0, 1)] public float weightNormal = 0.8f;
    [Range(0, 1)] public float weightArmored = 0.2f;
    [Range(0, 1)] public float weightPunishment = 0.0f;

    public bool IsInfinite => quota < 0;
}
