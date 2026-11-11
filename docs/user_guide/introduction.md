# Introduction

This guide introduces SysML2Workbench, a cross-platform Avalonia desktop

application for opening and exploring SysML v2 textual model workspaces. In its

current phase-0 scope, the product is focused on read-only model viewing,

predefined diagram browsing, custom view construction, and workspace diagnostics.

The typical workflow is shown below: open a workspace, browse the model, render

diagrams, then check diagnostics and export results.

![SysML2Workbench workflow in four steps: 1. open a workspace with File > Open Folder, File > Open File, or drag and drop, with live reload on external changes; 2. browse the model in the Workspace panel, Predefined Views list, read-only raw source text, and Query > Run Query; 3. render diagrams in tabs with pan and zoom, or build custom views with the Custom View Builder and element picker; 4. check the Diagnostics panel and export with Copy as SysML, Copy as Markdown or JSON, and the local log file for bug reports](workflow.svg)

## Purpose

The purpose of this guide is to help users and integrators understand what

SysML2Workbench does, what the current release is intended to support, and how to

begin working with a local SysML v2 workspace in the application.

## Scope

This guide currently covers:

- Installation expectations for the phase-0 desktop viewer

- Opening a workspace from one or more file/folder sources and browsing predefined views

- Building custom views by selecting a view type, multiple expose targets, and an optional filter

- Running queries over the workspace and copying query results as Markdown or JSON

- Reviewing parser and reference-resolution diagnostics for the workspace

- Current phase-0 limitations, including the absence of built-in git operations or model editing

Readers who are ready to set up the application should continue with the
Installation section, and then the Getting Started section for a walkthrough
of opening a workspace and building a custom view.

## References

- [REF-1] [SysML2Workbench releases](https://github.com/demaconsulting/SysML2Workbench/releases)

- [REF-2] Avalonia UI documentation, <https://docs.avaloniaui.net/>

- [REF-3] Object Management Group, Systems Modeling Language (SysML) v2, <https://www.omg.org/spec/SysML>
