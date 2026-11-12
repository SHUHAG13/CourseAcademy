# Business Docs Changelog

Format: `YYYY-MM-DD | <module> | <what changed> | <rule IDs affected>` — newest first.

2026-11-12 | all | data-model.md rewritten as SQL Server physical design (int identity PKs, soft delete, audit columns, UTC, decimal money, lookup tables incl. Screen/Permission, RefreshToken, lengths, indexes); api-contract.md rewritten with /api/v1, response wrapper, JWT auth, exact DTOs per endpoint; README technical decisions; APPLICATION-OVERVIEW B0/B12/B13/C1b synced. No business rule changes. | —
2026-11-12 | access-control | Access is now per whole screen: one checkbox per screen gives all its permissions (Overview includes Reset demo data); partial access saved as full screen. Per-course grants unchanged. | BR-ACL-12, BR-ACL-19
2026-11-09 | all | Added APPLICATION-OVERVIEW.md: combined whole-app document (Part A business, Part B technical, Part C open questions + likely bugs). No rule changes. | —
2026-11-09 | all | Initial business documentation generated from frontend demo (commit 7e5c7ff). | BR-AUTH-01–13, BR-ACL-01–18, BR-CAT-01–20, BR-CMS-01–18, BR-PAY-01–18, BR-ENR-01–10, BR-LRN-01–14, BR-CERT-01–16, BR-COM-01–16, BR-ADM-01–07, BR-SITE-01–09
