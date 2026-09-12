import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const project=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const data=path.join(project,'Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/src/main/assets/bin/Data');
const entries=fs.readdirSync(data).filter(name=>/^[a-f0-9]{32} [0-9]+$/.test(name))
  .filter(name=>fs.existsSync(path.join(data,name.replace(/ [0-9]+$/,''))));
const destination=path.join(project,'Verification/GradleQuarantine','v183-data-'+Date.now());
if(process.argv.includes('--apply')) {
  fs.mkdirSync(destination,{recursive:true});
  for(const name of entries)fs.renameSync(path.join(data,name),path.join(destination,name));
}
console.log(JSON.stringify({applied:process.argv.includes('--apply'),count:entries.length,destination,entries}));
