## Project handling

- [x] Reduce size of backups
- [x] Allow to override Project location
- [ ] Prevent creating projects with existing names
- [ ] unload projects from project list
- [ ] Load last project from user settings
- [x] Make sure that TiXL is using the high performance GPU

## Graph
- [x] Allow dragging connections from vertical output slot
- [x] Dragging gradient widget handles drags canvas too
- [x] Snapping connecting start to output not working of ops who's output is already snapped
- [ ] Add annotations
- [ ] Parameter window in fullscreen
- [ ] Fix background control in mag graph
- [ ] Allow dragging connection from horizontal input slot
- [ ] Allow clicking vertical input slot
- [ ] Split Connections
- [ ] Rewiring of vertical connection lines
- [ ] Panning/Zooming in CurveEdit-Popup opened from SampleCurveOp is broken 
- [ ] Publish as input does not create connection
- [ ] ShaderGraphNode should be bypassable
- [ ] In Parameter window bypassable button should be disabled if not available

## Timeline

- [ ] Soundtrack image is incorrectly scaled with playback?
- [ ] After deleting and restart recompilation of image is triggered, but image in timeline is not updated?
      Path not found: '/pixtur.still.Gheo/soundtrack/DARKrebooted-v1.0.mp3' (Resolved to '').

## UI-Scaling Issues (at x1.5):

- [ ] Perlin-Noise graph cut off
- [ ] Timeline-Clips too narrow
- [ ] Full-Screen cuts of timeline ruler units
- [ ] MagGraph-Labels too small
- [ ] Panning Canvas offset is scaled
- [ ] Pressing F12 twice does not restore the layout
- [ ] Snapping is too fine
- [ ] in Duplicate Symbol description field is too small

- [ ] Add some kind of FIT button to show all or selected operators 

## High frame-rate issues 120Hz

- [ ] Shake doesn't work with 120hz

## Ops

- [x] Remove Time 2nd output
- [ ] Rename Time2 <-> Time
- [ ] Rounded Rect should have blend parameter
- [ ] Fix BoxGradient
- [ ] SetEnvironment should automatically insert textureToCubemap
- [ ] Remove Symbol from Editor
- [ ] Fix SnapToPoints
- [ ] Sort out obsolete pixtur examples

## SDF-Stuff

- [ ] Changing the parameter order in the parameter window will break inputs with [GraphParam] attribute
- [ ] Ray marching glow
- [ ] Some for of parameter freezing
- [ ] Combine flood fill with 3d
- [ ] FieldToImage
- [ ] Flexible shader injection (e.g. DrawMesh normals, etc.)

## General fixes:

- [x] Fix camera handling
- [x] Default gradients are not loaded?
- [x] Fix Scaling for multiple selected keyframes with ALT-Key
- [x] Fix Gradient editor not working as parameter window parameter
- [x] Deleting last output will cause crash

## Documentation

- [ ] Fix WIKI export does not include input descriptions

## General UX-ideas:
- [ ] StatusProvideIcon should support non-warning indicator
- [ ] Separate Value Clamping for lower and upper values 
- [ ] Drag and drop of files (copy them to resources folder and create LoadXYZ instance...)



## Refactoring
- [ ] Remove ICanvas
- [ ] Refactor to use Scopes

## Long-Term ideas:
- [ ] Render-Settings should be a connection type, including texture sampling, culling, z-depth