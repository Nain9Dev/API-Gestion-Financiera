#!/usr/bin/env python3
"""Print a Markdown coverage summary from Cobertura reports produced by `dotnet test`.

Usage: coverage_summary.py <results-directory>

The script never fails the build: a missing or unreadable report degrades into a note, because
coverage is reported as information, not enforced as a gate.
"""

import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path


def percentage(value: str | None) -> str:
    try:
        return f"{float(value) * 100:.1f}%"
    except (TypeError, ValueError):
        return "n/a"


def main() -> int:
    if len(sys.argv) < 2:
        print("### Coverage\n\nNo results directory was provided.")
        return 0

    results = Path(sys.argv[1])
    reports = sorted(results.rglob("coverage.cobertura.xml"))

    print("### Coverage\n")

    if not reports:
        print(f"No Cobertura report was found under `{results}`.")
        return 0

    print("| Report | Lines | Branches |")
    print("| --- | ---: | ---: |")

    for report in reports:
        try:
            root = ElementTree.parse(report).getroot()
        except ElementTree.ParseError as error:
            print(f"| `{report.name}` | unreadable | {error.msg} |")
            continue

        label = report.parent.parent.name or report.name
        print(
            f"| `{label}` "
            f"| {percentage(root.get('line-rate'))} "
            f"| {percentage(root.get('branch-rate'))} |"
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
