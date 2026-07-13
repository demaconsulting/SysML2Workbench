# OTS Verification

## Verification Strategy

SysML2Workbench is a thin shell over several OTS components, so OTS verification is planned as
an evidence package that combines vendor maturity with repository-local integration tests.
Where a dependency directly defines user-visible behavior, the planned local evidence will live
under `test/OtsSoftwareTests/` and will exercise only the APIs and workflows that
SysML2Workbench actually depends on.

## Qualification Evidence

Each OTS item will be considered fit for purpose when the repository holds current integration
and usage design documentation, the planned OTS integration tests pass, and any dependency-
specific release notes or vendor documentation have been reviewed for the features used by
SysML2Workbench. For `XUnit`, qualification evidence will focus on successful test discovery,
execution, and reporting in the repository test harness rather than business-function behavior.

## Regression Approach

When any OTS package version changes, the planned response is to re-run the full local OTS test
suite, re-run affected local system or subsystem tests, and review upstream release notes for
behavioral or API changes that could alter verification assumptions. Any changed dependency that
invalidates existing test coverage will trigger updates to the relevant OTS and local
verification documents before the upgrade is accepted.
