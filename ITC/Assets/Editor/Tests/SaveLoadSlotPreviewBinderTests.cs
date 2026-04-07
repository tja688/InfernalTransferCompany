using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ITC.UIFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaveLoadSlotPreviewBinderTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> _createdObjects = new List<Object>();
    private readonly List<Object> _createdAssets = new List<Object>();

    [Test]
    public void HoveringSaveSlotStartsPreviewAndManualClearResetsIt()
    {
        var root = CreateRootCanvas();
        CreateEventSystem();

        var saveBoard = CreatePreviewBoard(root.transform,
            "存档菜单Canvas/存档菜单面板/背景板/提示图翻牌器-存档菜单",
            out var saveTextures);
        CreateButtonsRoot(root.transform,
            "存档菜单Canvas/存档菜单面板/界面组件/存储条",
            "存储栏-置顶");

        var binder = root.AddComponent<SaveLoadSlotPreviewBinder>();
        binder.Rebind();

        var proxy = root.transform
            .Find("存档菜单Canvas/存档菜单面板/界面组件/存储条/存储栏-置顶")
            .GetComponent<UIBehaviourProxy>();

        Assert.NotNull(proxy);

        var eventData = new PointerEventData(EventSystem.current);
        proxy.OnPointerEnter(eventData);

        Assert.IsTrue(saveBoard.IsTransitionActive);
        Assert.AreSame(saveTextures[0], GetTargetTexture(saveBoard));

        SetPrivateField(binder, "_currentHoverProxy", null);
        SetPrivateField(binder, "_currentHoverBoard", null);
        RunPendingClearImmediately(binder, saveBoard);
        Assert.IsNull(GetTargetTexture(saveBoard));
    }

    [Test]
    public void HoveringLoadSlotUsesSecondPlaceholderTexture()
    {
        var root = CreateRootCanvas();
        CreateEventSystem();

        var loadBoard = CreatePreviewBoard(root.transform,
            "加载菜单Canvas/存档菜单面板/背景板/提示图翻牌器-加载菜单",
            out var loadTextures);
        CreateButtonsRoot(root.transform,
            "加载菜单Canvas/存档菜单面板/界面组件/存储条",
            "读档条按钮模版");

        var binder = root.AddComponent<SaveLoadSlotPreviewBinder>();
        binder.Rebind();

        var proxy = root.transform
            .Find("加载菜单Canvas/存档菜单面板/界面组件/存储条/读档条按钮模版")
            .GetComponent<UIBehaviourProxy>();

        Assert.NotNull(proxy);

        proxy.OnPointerEnter(new PointerEventData(EventSystem.current));

        Assert.IsTrue(loadBoard.IsTransitionActive);
        Assert.AreSame(loadTextures[1], GetTargetTexture(loadBoard));
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
            {
                Object.DestroyImmediate(_createdObjects[i]);
            }
        }

        for (int i = _createdAssets.Count - 1; i >= 0; i--)
        {
            if (_createdAssets[i] != null)
            {
                Object.DestroyImmediate(_createdAssets[i]);
            }
        }
    }

    private GameObject CreateRootCanvas()
    {
        var root = new GameObject("根Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _createdObjects.Add(root);
        return root;
    }

    private void CreateEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        _createdObjects.Add(new GameObject("EventSystem", typeof(EventSystem)));
    }

    private UISolariBoard CreatePreviewBoard(Transform root, string relativePath, out Texture2D[] textures)
    {
        var boardTransform = EnsurePath(root, relativePath);
        boardTransform.gameObject.AddComponent<Image>();

        var content = new GameObject("content", typeof(RectTransform), typeof(GridLayoutGroup));
        content.transform.SetParent(boardTransform, false);
        _createdObjects.Add(content);

        var rawCell = new GameObject("Cell", typeof(RectTransform), typeof(RawImage));
        rawCell.transform.SetParent(content.transform, false);
        _createdObjects.Add(rawCell);

        var inspector = boardTransform.gameObject.AddComponent<UVInspector>();
        SetPrivateField(inspector, "contentRoot", content.GetComponent<RectTransform>());
        SetPrivateField(inspector, "gridLayout", content.GetComponent<GridLayoutGroup>());

        textures = new[]
        {
            BuildTexture(new Color32(255, 0, 0, 255)),
            BuildTexture(new Color32(0, 0, 255, 255))
        };

        SetPrivateField(inspector, "sourceTextures", new List<Texture>(textures));
        inspector.RefreshGrid(true);

        return boardTransform.gameObject.AddComponent<UISolariBoard>();
    }

    private void CreateButtonsRoot(Transform root, string relativePath, string buttonName)
    {
        var buttonsRoot = EnsurePath(root, relativePath);
        var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Animator));
        buttonObject.transform.SetParent(buttonsRoot, false);
        _createdObjects.Add(buttonObject);
    }

    private Transform EnsurePath(Transform root, string relativePath)
    {
        var segments = relativePath.Split('/');
        Transform current = root;

        for (int i = 0; i < segments.Length; i++)
        {
            var child = current.Find(segments[i]);
            if (child == null)
            {
                var gameObject = new GameObject(segments[i], typeof(RectTransform));
                child = gameObject.transform;
                child.SetParent(current, false);
                _createdObjects.Add(gameObject);
            }

            current = child;
        }

        return current;
    }

    private static Texture GetTargetTexture(UISolariBoard board)
    {
        return (Texture) typeof(UISolariBoard).GetField("_targetTexture", InstanceFlags)?.GetValue(board);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, InstanceFlags);
        Assert.NotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private Texture2D BuildTexture(Color32 color)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels32(new[] { color, color, color, color });
        texture.Apply();
        _createdAssets.Add(texture);
        return texture;
    }

    private static void RunPendingClearImmediately(SaveLoadSlotPreviewBinder binder, UISolariBoard previewBoard)
    {
        var method = binder.GetType().GetMethod("ClearPreviewNextFrame", InstanceFlags);
        Assert.NotNull(method, "ClearPreviewNextFrame was not found.");

        var routine = method.Invoke(binder, new object[] { previewBoard }) as IEnumerator;
        Assert.NotNull(routine);

        Assert.IsTrue(routine.MoveNext());
        Assert.IsFalse(routine.MoveNext());
    }
}
