# Source Generated with Decompyle++
# File: resource_finder.pyc (Python 3.12)

from __future__ import annotations
import json
import os
import plistlib
import sys
from pathlib import Path
from typing import Dict, List, Optional, Union
PathLike = Union[(str, Path)]
_MANIFEST_CANDIDATES = ('unifypy.json', 'app.json', 'package.json')

class ResourceFinder:
    pass
# WARNING: Decompyle incomplete

resource_finder = ResourceFinder()

def get_app_meta():
    return resource_finder.get_app_meta()


def get_app_name():
    return resource_finder.get_app_name()


def get_project_root():
    return resource_finder.get_project_root()


def get_user_data_dir(create = None):
    return resource_finder.get_user_data_dir(create)


def get_user_cache_dir(create = None):
    return resource_finder.get_user_cache_dir(create)


def find_file(p = None):
    return resource_finder.find_file(p)


def find_directory(p = None):
    return resource_finder.find_directory(p)


def find_models_dir():
    return resource_finder.find_models_dir()


def find_assets_dir():
    return resource_finder.find_assets_dir()


def find_config_dir():
    return resource_finder.find_config_dir()


def find_libs_dir(*, system, arch, *parts):
    pass
# WARNING: Decompyle incomplete


def find_models_subdir(*parts):
    pass
# WARNING: Decompyle incomplete


def find_assets_subpath(*parts):
    pass
# WARNING: Decompyle incomplete

