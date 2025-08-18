using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace HexLayersTest;

public class PlayerSpriteProvider
{
    public static string GetCharacterSprite(Vector2 direction)
    {
        if (direction.X > 0)
        {
            if (direction.Y > 0) return "walk_down_right";
            else if (direction.Y < 0) return "walk_up_right";
            else return "walk_right";
        }
        else if (direction.X < 0)
        {
            if (direction.Y > 0) return "walk_down_left";
            else if (direction.Y < 0) return "walk_up_left";
            else return "walk_left";
        }
        else
        {
            if (direction.Y > 0) return "walk_down";
            else if (direction.Y < 0) return "walk_up";
            else return "stand";
        }
    }
}
