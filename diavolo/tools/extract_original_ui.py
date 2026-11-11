"""Extract original UI texture chunks for the two survivor icon sets."""
import io, json, sys, hashlib
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps
sys.dont_write_bytecode=True
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "dio" / 'tools'))
from extract_dio import decompress
sys.path.insert(0,str(root/"tools/reference"))
from asbr_cpk_parser import crypt_jojo_asbr
sys.path.insert(0,str(Path(__file__).resolve().parents[2] / "dio" / 'third_party/xfbin-importer/Blender-XFBIN-Importer'))
from xfbin_lib.xfbin import read_xfbin, NuccChunkTexture
from xfbin_lib.xfbin.structure.dds import NutTexture_to_DDS
out=root/"assets/original-ui"
records=[]
for game,index in [("eoh",Path(str(Path(__file__).resolve().parents[2] / "dio" / 'docs/cpk-contents.json'))),("asbr",root/"docs/asbr-cpk-contents.json")]:
    chosen={}
    for archive in json.loads(index.read_text(encoding="utf-8-sig")):
        for e in archive.get("entries",[]):
            name=e.get("DirName","")+"/"+e["FileName"]
            character=any(c in name for c in ["3dio01","3wld01","5dvl01","5kcr01"])
            wanted=character and any(s in name for s in ["/face_card/","/face_player/","/cutin/","/icon_s/","/duel/char/","/btlsel/char/"])
            wanted|=name in ["data/ui/btl/btl_cmn.xfbin","data/ui/btl/btl_stand.xfbin","data/ui/duel/btl_cmn.xfbin"]
            if wanted and name.endswith(".xfbin"):chosen[name]=(archive["archive"],e)
    for name,(archive,e) in chosen.items():
        with Path(archive).open("rb") as stream:
            stream.seek(e["AbsoluteOffset"]);data=stream.read(e["FileSize"])
        if game=="asbr":data=crypt_jojo_asbr(data)
        data=decompress(data)
        assert len(data)==e["ExtractSize"],name
        source=out/game/name
        source.parent.mkdir(parents=True,exist_ok=True);source.write_bytes(data)
        try:
            for chunk in read_xfbin(str(source)).get_chunks_by_type(NuccChunkTexture):
                if not hasattr(chunk,"nut"):continue
                for i,t in enumerate(chunk.nut.textures):
                    image=Image.open(io.BytesIO(NutTexture_to_DDS(t))).convert("RGBA")
                    output=source.parent/(source.stem+"__"+chunk.name.replace("/","_").replace("\\","_")+f"_{i}.png")
                    image.save(output)
                    records.append(dict(game=game,entry=name,chunk=chunk.name,index=i,size=list(image.size),png=str(output.relative_to(root)),sha256=hashlib.sha256(data).hexdigest()))
        except Exception as ex:print("DECODE ERROR",name,str(ex))
(root/"docs/original-ui-textures.json").write_text(json.dumps(records,indent=2))
thumbs=[]
for i,r in enumerate(records):
    image=Image.open(root/r["png"])
    tile=Image.new("RGB",(240,210),(35,35,40))
    thumb=ImageOps.contain(image,(232,166))
    tile.paste(thumb,((240-thumb.width)//2,0),thumb)
    d=ImageDraw.Draw(tile);d.text((4,168),f"{i} {r['game']} {r['size']}",fill="white")
    d.text((4,183),r["chunk"][:31],fill="white")
    thumbs.append(tile)
for start in range(0,len(thumbs),30):
    tiles=thumbs[start:start+30]
    sheet=Image.new("RGB",(240*5,210*((len(tiles)+4)//5)),(20,20,20))
    for i,t in enumerate(tiles):sheet.paste(t,((i%5)*240,(i//5)*210))
    sheet.save(root/f"docs/original-ui-sheet-{start//30}.jpg")
print("Extracted",len(records),"textures")
