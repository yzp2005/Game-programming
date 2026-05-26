using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class planemove : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 8f;

    void Update()
    {
        // --- 1. WASD 移动部分 ---
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S
        Vector2 moveDir = new Vector2(h, v).normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime);

        // --- 2. 朝向鼠标部分 ---
        // 把鼠标屏幕坐标转为世界坐标
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 计算飞机到鼠标的方向向量
        Vector2 lookDir = mousePos - transform.position;
        // 计算角度（弧度转角度）
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        // 让飞机旋转（Unity默认朝向是右，所以减去90°让它默认朝上）
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}