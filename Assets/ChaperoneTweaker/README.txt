# ChaperoneTweaker by Haï~ (@vr_hai github.com/hai-vr)

This a tool I wrote to edit my SteamVR Chaperone bounds inside Unity Editor.
It allowed me to redefine precise boundaries for my own needs.

## How to use

- Close SteamVR.
- Open your `Steam/config` folder.
- Make a backup of the `Steam/config/chaperone_info.vrchap` file!
- Copy `Steam/config/chaperone_info.vrchap` to anywhere in the `Assets/` folder.
- Create a new scene or reuse an existing scene.
- Create a new GameObject at the root of the hierarchy.
- Add a `Chaperone Tweaker` component.
- Drag and drop the `chaperone_info.vrchap` Asset to the field.
- Click *Load*.
  - If the chaperone file contains multiple Universes (SteamVR Base Station configurations), select one of them to preview it in the scene.
  - Click *Confirm universe selection*.
- Move the children GameObject spheres, delete, or add new ones.
- Select the `Chaperone Tweaker` component to redraw the line renderer.
- When done, click *Overwrite asset with new positions*.
- Drag and drop the Asset back to the `Steam/config` folder to overwrite the `Steam/config/chaperone_info.vrchap` file.

## Notes and tips

- Rotate the GameObject that contains the `Chaperone Tweaker` component to line your room with the Unity grid.
- Only leaf GameObjects are used to build the chaperone bounds: If one of the GameObjects has children, only its children will be used to build the bounds, not the parent.
  - Using this, you can reparent the children to another GameObject in order to manipulate them more easily, especially if you lined up your room with the grid earlier.
- A quick way to line things up is to drag-select multiple objects and rescale them on a world axis using *Center/Global* mode.

## Notes

- The names of the GameObjects do not matter.
- When loading a `chaperone_info.vrchap` Asset, its contents become permanently saved inside the component. Replacing the Asset with a new `chaperone_info.vrchap` Asset will not have any effect. If you need to perform additional edits in the far future, it is advised to delete the Asset and start from scratch.
