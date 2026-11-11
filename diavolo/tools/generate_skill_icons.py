"""Author simple vector pictograms and rasterize the same primitives for Unity."""
from pathlib import Path
import sys, math
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'.tools/speech'))
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[1]
output=root/'UnityProject/Assets/Generated/Icons'
sources=root/'assets/skill-icons';sources.mkdir(exist_ok=True)
S=4;ink='#f5dbe4';red='#e74363';bg='#160b12'
class Icon:
 def __init__(self,name):
  self.name=name;self.image=Image.new('RGB',(128*S,128*S),bg);self.d=ImageDraw.Draw(self.image);self.svg=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><rect width="128" height="128" rx="12" fill="{bg}"/>']
 def line(self,points,color=ink,width=6,closed=False,fill=None):
  pts=[(int(x*S),int(y*S)) for x,y in points]
  if fill:self.d.polygon(pts,fill=fill)
  self.d.line(pts+([pts[0]] if closed else []),fill=color,width=width*S,joint='curve')
  self.svg.append(f'<{"polygon" if closed else "polyline"} points="'+ ' '.join(f'{x},{y}' for x,y in points)+f'" fill="{fill or "none"}" stroke="{color}" stroke-width="{width}" stroke-linejoin="round" stroke-linecap="round"/>')
 def circle(self,x,y,r,color=ink,width=5,fill=None):
  self.d.ellipse(((x-r)*S,(y-r)*S,(x+r)*S,(y+r)*S),outline=color,width=width*S,fill=fill)
  self.svg.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill or "none"}" stroke="{color}" stroke-width="{width}"/>')
 def arc(self,x,y,r,start,end,color=ink,width=6):
  self.line([(x+math.cos(math.radians(a))*r,y+math.sin(math.radians(a))*r) for a in range(start,end+1,4)],color,width)
 def fist(self,x=36,y=25,k=1,color=ink):
  def tr(pts):return [(x+a*k,y+b*k) for a,b in pts]
  self.line(tr([(4,48),(0,17),(7,9),(16,10),(20,3),(30,3),(35,0),(46,2),(51,8),(60,11),(63,30),(57,47),(47,59),(13,59),(4,48)]),color,5,True)
  for xx in [16,30,45]:self.line(tr([(xx,10),(xx,26)]),color,3)
  self.line(tr([(7,33),(24,28),(35,35),(32,43),(18,45)]),color,4)
 def arrow(self,a,b,color=red,width=6):
  self.line([a,b],color,width);dx,dy=b[0]-a[0],b[1]-a[1];l=math.hypot(dx,dy);dx,dy=dx/l,dy/l
  self.line([(b[0]-dx*12-dy*8,b[1]-dy*12+dx*8),b,(b[0]-dx*12+dy*8,b[1]-dy*12-dx*8)],color,width)
 def drop(self,x,y,k=1):
  self.line([(x,y-18*k),(x-12*k,y+3*k),(x-11*k,y+13*k),(x-5*k,y+19*k),(x+5*k,y+19*k),(x+11*k,y+13*k),(x+12*k,y+3*k)],red,4,True,red)
 def save(self):
  self.image.resize((128,128),Image.Resampling.LANCZOS).save(output/(self.name+'.png'))
  (sources/(self.name+'.svg')).write_text(''.join(self.svg)+'</svg>')
icons=[]
for name in ['CHAIN','CHOP','SMASH','IMPALE','SKIP','ERASE','EPITAPH','BLOOD','BARRAGE','COUNTER','GRAB','CANCEL','ULTIMATE','BLOCK','JUMP']:
 i=Icon(name)
 if name=='BLOCK':
  i.line([(64,15),(105,33),(99,76),(64,112),(29,76),(23,33)],ink,6,True);i.line([(64,35),(64,87)],red,7)
 elif name=='JUMP':
  i.arrow((64,99),(64,22));i.line([(22,105),(22,86),(42,86)],ink,6);i.line([(86,86),(106,86),(106,105)],ink,6)
 elif name=='CHAIN':
  i.fist(40,20,.85);i.arrow((18,88),(48,88));i.arrow((48,103),(78,103));i.arrow((78,88),(108,88))
 elif name=='CHOP':
  i.line([(34,22),(48,16),(89,63),(89,74),(76,84),(62,78),(21,38)],ink,6,True);i.line([(25,62),(47,84)],red,5);i.line([(14,74),(40,100)],red,5);i.line([(70,96),(82,108),(100,97)],red,5)
 elif name=='SMASH':
  i.fist(40,12,.8);i.arrow((22,36),(22,77));i.line([(12,101),(44,101),(52,89),(66,112),(77,94),(91,101),(116,101)],red,6)
 elif name=='IMPALE':
  i.fist(15,23,.83);i.arrow((61,62),(110,62));i.line([(96,26),(87,43)],ink,5);i.line([(96,98),(87,81)],ink,5);i.drop(60,99,.55)
 elif name=='SKIP':
  i.arc(64,63,43,115,230,ink,5);i.arc(64,63,43,-65,50,ink,5);i.arrow((28,55),(72,55));i.arrow((57,78),(101,78));i.line([(64,26),(64,39)],ink,4)
 elif name=='ERASE':
  i.arc(58,60,39,105,265,ink,6);i.arc(70,67,39,-75,85,ink,6);i.line([(58,30),(58,60),(38,60)],ink,5);i.line([(74,16),(56,49),(75,69),(53,110)],red,7)
 elif name=='EPITAPH':
  i.line([(12,64),(32,42),(64,30),(96,42),(116,64),(96,86),(64,98),(32,86)],ink,5,True);i.circle(64,64,22,red,5);i.line([(64,49),(75,64),(64,79),(53,64)],red,3,True,red)
 elif name=='BLOOD':
  i.drop(64,56,1.55);i.drop(26,86,.65);i.drop(102,86,.65)
 elif name=='BARRAGE':
  i.fist(12,17,.63);i.fist(65,20,.63);i.fist(40,65,.63);i.line([(15,77),(29,69)],red,5);i.line([(92,100),(110,88)],red,5)
 elif name=='COUNTER':
  i.line([(64,17),(98,30),(94,74),(80,94),(64,108),(48,94),(34,74),(30,30)],ink,5,True);i.arrow((92,56),(45,56));i.line([(89,39),(102,48),(102,68),(89,77)],red,5)
 elif name=='GRAB':
  i.fist(19,26,.88);i.arc(68,65,40,-80,15,red,6);i.arrow((106,73),(94,97));i.line([(28,94),(51,111),(69,96)],ink,5)
 elif name=='CANCEL':
  i.arc(64,64,42,10,160,ink,5);i.arc(64,64,42,190,340,ink,5);i.line([(42,42),(86,86)],red,9);i.line([(86,42),(42,86)],red,9)
 elif name=='ULTIMATE':
  for x,h in [(32,37),(64,17),(96,37)]:i.line([(x-10,100),(x,h),(x+10,100)],ink,4,True,red)
  i.line([(14,106),(114,106)],ink,5)
 i.save();icons.append(i)
sheet=Image.new('RGB',(7*160,3*160),bg);draw=ImageDraw.Draw(sheet)
for n,i in enumerate(icons):
 x=(n%7)*160;y=(n//7)*160;sheet.paste(i.image.resize((128,128),Image.Resampling.LANCZOS),(x+16,y));draw.text((x+16,y+136),i.name,fill=ink)
sheet.save(root/'docs/skill-symbols.png')
print('Generated 15 SVG pictograms and Unity PNGs')
