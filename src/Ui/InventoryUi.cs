using System;
using System.Collections.Generic;
using Godot;

public class InventoryUi
{
    public const int SLOTS_AMOUNT = 6;
    public const float SLOT_RELATIVE_HEIGHT = 0.5f;
    public const float SLOT_GAP = 13.5f;
    public const float SLOT_MARGIN_LEFT = 15;
    public const float ROTATION_SPEED = 1;
    public const float INVENTORY_MARGIN_BOTTOM = 20;
    public const float TEXT_MARGIN_LEFT = 150;
    public const float TEXT_MARGIN_RIGHT = 10;

    private readonly Session _session;
    private readonly HashSet<int> _oobSlotsSet = [];
    private int _oobAmount = 0;
    private readonly List<Spatial> _inventorySlotParents = [];
    private readonly List<Vector3> _inventorySlotRotations = [];

    private readonly Random _rng;

    private readonly RichTextLabel _amountTextLabel;

    private Vector2 UiSize => _session.RootUi.RectSize;

    public float SlotSize => _inventoryTextureRect.RectSize.y;

    private readonly TextureRect _inventoryTextureRect;

    public InventoryUi(Session session, Random? rng = null)
    {
        _rng = rng ?? new();

        _session = session;
        session.Update += Update;

        var inventoryBox = GD.Load<Texture>("res://Assets/Textures/Ui/InventoryBox.png");
        _inventoryTextureRect = new TextureRect { Texture = inventoryBox };
        var text = new RichTextLabel();
        _inventoryTextureRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        _session.RootUi.AddChild(_inventoryTextureRect);
        _inventoryTextureRect.RectPosition = new(
            (UiSize.x - _inventoryTextureRect.RectSize.x) / 2,
            UiSize.y - _inventoryTextureRect.RectSize.y - INVENTORY_MARGIN_BOTTOM
        );
        _inventoryTextureRect.AddChild(text);
        text.SetAnchorsAndMarginsPreset(Control.LayoutPreset.CenterRight);
        text.MarginLeft = -TEXT_MARGIN_LEFT;
        text.MarginRight = -TEXT_MARGIN_RIGHT;
        text.FitContentHeight = true;
        text.RectScale = new(3, 3);
        // var font = new DynamicFont { Size = 24 };
        // text.AddFontOverride("normal_font", font);
        _amountTextLabel = text;
        var viewportContainer = new ViewportContainer();
        var viewport = new Viewport();
        viewportContainer.AddChild(viewport);
        viewportContainer.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
        viewport.TransparentBg = true;
        viewport.World = new();
        viewport.AddChild(new DirectionalLight());
        viewportContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        _inventoryTextureRect.AddChild(viewportContainer);
        viewport.Size = viewportContainer.RectSize;
        var camera = new Camera();
        var cameraPosition = camera.Position;
        cameraPosition.x += viewport.Size.x / 2;
        camera.Position = cameraPosition;
        viewport.AddChild(camera);
        camera.SetOrthogonal(viewport.Size.y, 0, 1000);

        for (var i = 0; i < SLOTS_AMOUNT; i++)
        {
            var slotSize = SLOT_RELATIVE_HEIGHT * _inventoryTextureRect.RectSize.y;
            var slotPositionX = SLOT_MARGIN_LEFT + slotSize * i + SLOT_GAP * i;
            var slotPositionY = _inventoryTextureRect.RectSize.y / 2;
            var slotParent = new Spatial { Position = new(slotPositionX, 0, -500) };
            viewport.AddChild(slotParent);
            _inventorySlotParents.Add(slotParent);
            _inventorySlotRotations.Add(
                new Vector3(
                    (float)_rng.NextDouble(),
                    (float)_rng.NextDouble(),
                    (float)_rng.NextDouble()
                ) * 2
                    - Vector3.One
            );
        }
        viewport.Size = viewport.Size with { x = viewport.Size.x + SlotSize };
    }

    private void Update(float delta)
    {
        _amountTextLabel.RectPosition = _amountTextLabel.RectPosition with
        {
            y = (_inventoryTextureRect.RectSize.y - _amountTextLabel.RectSize.y * 3) / 2,
        };
        for (int i = 0; i < _inventorySlotParents.Count; i++)
        {
            var parent = _inventorySlotParents[i];
            parent.Rotation += Vector3.One * delta * ROTATION_SPEED * _inventorySlotRotations[i];
        }
    }

    public void Update(InventoryActionSlotSet action)
    {
        SetSlotItem(action.SlotIndex, action.ItemStack);
    }

    private void SetSlotItem(int id, ItemStack? stack)
    {
        if (id >= SLOTS_AMOUNT)
        {
            if (stack != null)
            {
                _oobSlotsSet.Add(id);
            }
            else
            {
                _oobSlotsSet.Remove(id);
            }
            _oobAmount = _oobSlotsSet.Count;
            _amountTextLabel.Text = _oobAmount == 0 ? "" : "+" + _oobAmount.ToString();
            return;
        }
        else
        {
            var parent = _inventorySlotParents[id];
            GD.Print("Id: ", id);
            foreach (var child in parent.GetChildren())
            {
                ((Node)child).QueueFree();
            }
            if (stack.HasValue)
            {
                var node = (MeshInstance)stack.Value.Item.MeshInstance.Duplicate();
                var nodeScale = SlotSize / node.GetAabb().Size.Max() * SLOT_RELATIVE_HEIGHT;
                node.Scale = new(nodeScale, nodeScale, nodeScale);
                parent.AddChild(node);
            }
        }
    }
}
