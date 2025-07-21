using System;
using System.Linq;
using Terraria;
using WorldEdit.Expressions;

namespace WorldEdit;

public class PlayerInfo
{
	private int _x = -1;

	private int _x2 = -1;

	private int _y = -1;

	private int _y2 = -1;

	public const string Key = "WorldEdit_Data";

	public int Point = 0;

	public Selection? Select = null;

	private MagicWand? _magicWand = null;

	public Expression? SavedExpression = null;

	public int X
    {
        get => this._x;
        set
        {
            this._x = Math.Max(0, value);
            this._magicWand = null;
        }
    }

    public int X2
    {
        get => this._x2;
        set
        {
            this._x2 = Math.Min(value, Main.maxTilesX - 1);
            this._magicWand = null;
        }
    }

    public int Y
    {
        get => this._y;
        set
        {
            this._y = Math.Max(0, value);
            this._magicWand = null;
        }
    }

    public int Y2
    {
        get => this._y2;
        set
        {
            this._y2 = Math.Min(value, Main.maxTilesY - 1);
            this._magicWand = null;
        }
    }

    public MagicWand MagicWand
    {
        get => this._magicWand ?? new MagicWand();
        set
        {
            this._magicWand = value ?? new MagicWand();
            if (value == null)
            {
                this._x = this._x2 = this._y = this._y2 = -1;
                return;
            }
            var source = this._magicWand.Points.OrderBy((WEPoint p) => p.X);
            var source2 = this._magicWand.Points.OrderBy((WEPoint p) => p.Y);
            this._x = source.First().X;
            this._x2 = source.Last().X;
            this._y = source2.First().Y;
            this._y2 = source2.Last().Y;
        }
    }
}
