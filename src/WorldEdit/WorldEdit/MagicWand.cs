using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit;

public class MagicWand
{
    public static int MaxPointCount;

    internal bool dontCheck = false;

    internal List<WEPoint> Points = new List<WEPoint>();

    public MagicWand()
    {
        this.dontCheck = true;
    }

    public MagicWand(WEPoint[] Points)
    {
        this.dontCheck = false;
        this.Points = Points?.ToList() ?? new List<WEPoint>();
    }

    public bool InSelection(int X, int Y)
    {
        return this.dontCheck || this.Points.Any(p => p.X == X && p.Y == Y);
    }

    public static bool GetMagicWandSelection(int X, int Y, Expression Expression, TSPlayer Player, out MagicWand MagicWand)
    {
        MagicWand = new MagicWand();
        if (!Tools.InMapBoundaries(X, Y) || Expression == null)
        {
            return false;
        }
        if (!Expression.Evaluate(Main.tile[X, Y]))
        {
            return false;
        }

        var x = (short) X;
        var y = (short) Y;
        var points = new List<WEPoint> { new WEPoint(x, y) };
        var count = 0;
        var visited = new bool[Main.maxTilesX, Main.maxTilesY];
        visited[x, y] = true;

        for (var i = 0; i < points.Count; i++)
        {
            var current = points[i];
            var neighbors = new WEPoint[]
            {
                new WEPoint((short)(current.X + 1), current.Y),
                new WEPoint((short)(current.X - 1), current.Y),
                new WEPoint(current.X, (short)(current.Y + 1)),
                new WEPoint(current.X, (short)(current.Y - 1))
            };

            foreach (var neighbor in neighbors)
            {
                if (Tools.InMapBoundaries(neighbor.X, neighbor.Y) && !visited[neighbor.X, neighbor.Y])
                {
                    visited[neighbor.X, neighbor.Y] = true;
                    if (Expression.Evaluate(Main.tile[neighbor.X, neighbor.Y]))
                    {
                        points.Add(neighbor);
                        count++;
                        if (count >= MaxPointCount)
                        {
                            Player.SendErrorMessage("Hard selection tile limit " + $"({MaxPointCount}) has been reached.");
                            return false;
                        }
                    }
                }
            }
        }

        MagicWand = new MagicWand(points.ToArray());
        return true;
    }
}