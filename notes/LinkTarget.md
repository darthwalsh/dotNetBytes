Context: https://github.com/darthwalsh/dotNetBytes/tree/wip-linktarget is trying to solve javascript TODO:

```
//TODO(LINK) link targets, using dim? What if both?
```
Out of scope is viewing the doing something with linked [[#Size]], but that is a good followup goal

(WIP Branch introduced a few more TODOs for doing pre-order traversal which is good for conflicts?, but...)  the bigger problem is that the targets can be huge, i.e. the entire Section\[1\] in https://dotnet.carlwa.com/?Example=true#FileFormat/PEHeader/SectionHeaders[1]/PointerToRawData which *should be linked*
```
// TODO pre-order traversal start drawing the biggest link targets
// TODO then draw links themselves pre-order
// TODO draw link targets using dim
// TODO some kind of preview on hover?
```

#### Huge Drawing Solution?
Instead of setting byte background color, instead set the starting byte top-left-bottom margin/padding to color, and ditto ending byte top-right-bottom

## Followups

### Size
- [ ] Often have RVAandSize as two fields
- [ ] Size could somehow link to the end, inclusive of field

