// SequencerCommandBias.cs
// 自定义序列命令：Bias(target, toProgress, duration)
// 例：Bias(图层170, 1, 1)

using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandBias : SequencerCommand
{
    private readonly int _progressID = Shader.PropertyToID("_Progress");

    private List<Material> _materials = new List<Material>();
    private float _from = 0f;
    private float _to = 1f;
    private float _duration = 1f;
    private float _elapsed = 0f;
    private bool _valid = false;

    public void Start()
    {
        // 参数解析
        // 参数0：目标对象(支持 speaker、listener、对象名、路径等 Sequence 规则)
        Transform subject = GetSubject(0);
        if (subject == null)
        {
            // 兜底：按名字寻找
            var nameArg = GetParameter(0);
            if (!string.IsNullOrEmpty(nameArg))
            {
                var go = GameObject.Find(nameArg);
                if (go != null) subject = go.transform;
            }
        }

        _to = GetParameterAsFloat(1, 1f);
        _duration = Mathf.Max(0.0001f, GetParameterAsFloat(2, 1f));

        if (subject == null)
        {
            Debug.LogWarning("[SequencerCommandBias] Target not found.");
            Stop(); // 结束命令
            return;
        }

        // 收集 Renderers，并获取具有 _Progress 的材质（实例化材质）
        _materials.Clear();
        var renderers = subject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // 仅处理常见 2D/3D 渲染器
            if (!(r is SpriteRenderer) && !(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;

            // 实例化材质，避免污染 sharedMaterial
            var m = r.material;
            if (m != null && m.HasProperty(_progressID))
            {
                _materials.Add(m);
            }
        }

        if (_materials.Count == 0)
        {
            Debug.LogWarning("[SequencerCommandBias] No material with _Progress found on target.");
            Stop();
            return;
        }

        // 以首个材质当前值为 from
        _from = _materials[0].GetFloat(_progressID);
        _elapsed = 0f;
        _valid = true;
    }

    public void Update()
    {
        if (!_valid) return;

        _elapsed += DialogueTime.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // 平滑缓动（S-curve: smoothstep-ish）
        float e = t * t * (3f - 2f * t);
        float p = Mathf.Lerp(_from, _to, e);

        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i] != null)
            {
                _materials[i].SetFloat(_progressID, p);
            }
        }

        if (t >= 1f)
        {
            Stop(); // 动画完成，结束命令
        }
    }

    public void OnDestroy()
    {
        // 命令结束。这里通常无需回收材质（材质实例会随对象销毁时清理）。
        _materials.Clear();
    }
}
