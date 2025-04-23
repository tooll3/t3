## Refactor project handling 

- [ ] unload projects from project list
- [ ] Load last project from user settings

## Graph

- [ ] Parameter window in fullscreen
- [ ] Fix background control in mag graph
- [ ] Add annotations
- [ ] Allow dragging connection from horizontal input slot
- [ ] Allow clicking vertical input slot
- [x] Allow dragging connections from vertical output slot
- [ ] Split Connections
- [ ] Rewiring of vertical connection lines
- [x] Dragging gradient widget handles drags canvas too
- [ ] Snapping connecting start to output not working of ops who's output is already snapped
- [ ] Panning/Zooming in CurveEdit-Popup opened from SampleCurveOp is broken 

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

## Ops:
- [ ] Rounded Rect should have blend parameter
- [ ] SetEnvironment should automatically insert textureToCubemap

General fixes:
- [x] Fix camera handling
- [x] Default gradients are not loaded?
- [x] Fix Scaling for multiple selected keyframes with ALT-Key
- [ ] Deleting last output will cause crash
- [ ] Shake doesn't work with 120hz

General UX-ideas:
- [ ] StatusProvideIcon should support non-warning indicator
- [ ] Separate Value Clamping for lower and upper values 
- [ ] Drag and drop of files (copy them to resources folder and create LoadXYZ instance...)

Feats:
- [x] Copy and Paste Values
- [x] Maybe show tags in symbolBrowser / Placeholder

Ops
- [ ] Fix BoxGradient
- [x] Remove Time 2nd output
- [ ] Fix SnapToPoints
- [ ] Sort out obsolete pixtur examples

Refactoring
- [ ] Remove ICanvas
- [ ] Refactor to use Scopes
- 
Long-Term ideas:
- [ ] Render-Settings should be a connection type, including texture sampling, culling, z-depth