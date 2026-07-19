# Changelog

## [1.0.0] - 2026-07-18

### Added
- `InteractionObjectTaker` — ray-based selection, pick-up, hold and throw driven by
  `Interaction()` and `ThrowObject()`.
- `InteractionObject` — marks a physics object as interactable with a configurable
  hover highlight (colour, width, mode).
- Automatic release of a held object when it goes behind geometry on a configurable
  `Wall Layers` mask.
- Original inverted-hull `Outline` component built from two single-pass materials
  (mask + fill), rendering correctly in both the Built-in Render Pipeline and URP.
- Assembly definition `SST.InteractionObjects`.
- UPM package layout (`package.json`, `Runtime/`, `Documentation~/`, `Samples`) and a
  `PlayerInteractionInput` demo controller.
- Bilingual documentation (English and Russian).

### Changed
- All runtime types moved to the `SST.InteractionObjects` namespace.
- Internal helpers (`HandJointController`, `ObjectRaycaster<T>`) reorganised under
  `Runtime/Internal` and cleaned up (naming, docs, reduced per-tick allocations).
