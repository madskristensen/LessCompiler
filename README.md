# CSS AutoPrefixer

[![Build status](https://ci.appveyor.com/api/projects/status/uh1b5p1wx3ld64r9?svg=true)](https://ci.appveyor.com/project/madskristensen/lesscompiler)

Download this extension from the [Marketplace](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.LESSCompiler)
or get the [CI build](http://vsixgallery.com/extension/d32c5250-fa82-4da6-9732-5518fabebfef/).

---------------------------------------

An alternative LESS compiler with no setup. Uses the official node.js based LESS compiler under the hood with AutoPrefixer and CSSComb built in.

See the [change log](CHANGELOG.md) for changes and road map.

## Features

- Compiles .less files on save
- Uses the [official LESS](https://www.npmjs.com/package/less) node module
- Automatially runs [autoprefix](https://www.npmjs.com/package/less-plugin-autoprefix)
- Automatically runs [CSSComb](https://www.npmjs.com/package/less-plugin-csscomb)
- All compiler options configurable

### Compile on save
All .less files will automatically be compiled into a .css file nested under it in Solution Explorer.

![Solution Explorer](art/solution-explorer.png)

The automatic compilation doesn't happen if:

1. The .less file starts with an `_` like `_variables.less`
2. The .less file isn't part of any project
3. A comment in the .less file with the word `no-compile` is found

## Contribute
Check out the [contribution guidelines](.github/CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the
[Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project.

## License
[Apache 2.0](LICENSE)