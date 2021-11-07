#!/bin/bash

ls ./Assets/Scripts/*.cs | entr osascript -e 'activate application "Unity"' -e 'activate application "Visual Studio"' 

