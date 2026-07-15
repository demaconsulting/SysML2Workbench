# Installation

SysML2Workbench is intended to run on Windows, Linux, and macOS. Distribution and

packaging details are still evolving, so phase-0 use is currently centered on

running a local build or a published desktop package when one is provided.

## Prerequisites

- A supported desktop operating system: Windows, Linux, or macOS

- Access to the SysML2Workbench build or release artifacts

- A compatible .NET environment when running an unpublished local build

## Current Installation Model

At this stage, the most reliable setup path is to use the repository's documented

build toolchain and then launch the desktop application from the resulting build

output. When packaged releases are available, use the release-specific instructions

provided with that distribution.

## Notes for Integrators

Integrators embedding SysML2Workbench into a larger workflow should treat the

application as a local desktop client over a workspace folder. The application does

not currently require server-side services, user accounts, or network connectivity.

Once the application is installed and running, continue with the Getting
Started section for a walkthrough of opening a workspace and building a
custom view.
