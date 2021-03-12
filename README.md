# RECV_Editor
Tool to extract and edit texts and other localizable data from Resident Evil: Code Veronica for PS2 and, in the future, for the other versions too. It's still in a very alpha state.

## Requirements
The program requires the .NET Framework 4.7.2.

## RDX Format

Most RDX files contain a magic `0x41200000` (Magic1), but some of them contain `0x40051eb8` (Magic2).

An RDX file contains 5 data blocks. Their pointers are located at `0x10`.

* Block 0
    * Always 16 sub-blocks.
    * Some sub-block pointers can be 0, so there's no data there.
    * Sub-block 14 contains texts, unless its pointer is 0.
    * If the RDX has Magic2, the pointer to sub-block 12 is an unknown value and not a pointer.

* Block 1
    * Variable number of sub-blocks.
    * Pointers have an alignment of `0x40` bytes.
    * Pointers are always consecutive (no zeroes in the middle).
    * Pointers always end when reaching the first 0, or when reaching the data of the first sub-block (always "MDL").

* Block 2
    * Variable number of sub-blocks.
    * Pointers can be non-consecutive (there can be zeroes in the middle).
    * Sometimes it contains a completely different type of data without pointers at the beginning.

* Block 3

* Block 4
    * It has multiple entries with TIM2 containers.
    * Those containers can have multiple TIM2 textures, but those TIM2 are a bit unconventional. They have an extra header of `0x20` bytes. Removing it, it becomes a normal TIM2.
    * Sometimes, there is a PLI section before the TIM2.
    * There is one case, in RDX #87, that there is a null file of `0x20` bytes and a magic `0xFFFFFFFF`.