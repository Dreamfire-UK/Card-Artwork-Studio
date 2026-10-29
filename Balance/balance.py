"""Five Roads prototype: deterministic board simulation and authored-card inventory.

Python 3.10+, standard library only. This is an explicit experimental rules profile,

not a claim that the game's unresolved rules or competitive balance are final.

"""

from __future__ import annotations

import argparse, collections, copy, hashlib, html, json, math, random

from dataclasses import dataclass

from pathlib import Path



RULES = dict(life=20, hand=5, actions=2, max_turns=80, rear_cap=3,

             draw_first=False, seed=20261029, scars=2, arrows=True)

SUPPORTED = {'Seedling Attendant','Thornwatch Stag','Vine Scout','Briar Sprout',

 'Briar Warden','Grove Mender','Walking Thicket','Heartwood Herald','Yggdrasil, Living Citadel',

 'Grave Moth','Shade Imp','Webling','Hollow Skitter','Silkspinner','Ruin Spectre',

 'Widow Matron','Broodgate Demon','Queen of the Unclaimed Path'}

DELTAS = [(0,1),(1,1),(1,0),(1,-1),(0,-1),(-1,-1),(-1,0),(-1,1)]



def load(path):

    data=json.loads(Path(path).read_text(encoding='utf-8-sig'))

    for d in data['decks']:

        for e in d['main']+d['evolution']: e['card']=json.loads(e['cardJson'])

    for e in data.get('variants',[]): e['card']=json.loads(e['cardJson'])

    return data



def validate(data):

    errors=[]

    for d in data['decks']:

        if sum(e['quantity'] for e in d['main'])!=20: errors.append(d['name']+': main deck must contain 20 cards')

        if sum(e['quantity'] for e in d['evolution'])!=1: errors.append(d['name']+': evolution deck must contain one card')

        names={e['card']['cardName'] for e in d['main']}

        for e in d['main']+d['evolution']:

            c=e['card']

            if c['cardName'] not in SUPPORTED: errors.append('No simulation adapter: '+c['cardName'])

            if e['quantity']<1 or c['power']<0 or c['defense']<1: errors.append('Invalid quantity/stats: '+c['cardName'])

            if c['kind']==1 and e['evolvesFrom'] not in names: errors.append('Missing evolution base: '+c['cardName'])

            if c.get('secondEssence',0): errors.append('Mixed-Essence payment not yet supported: '+c['cardName'])

            if not e['artworkPath']: errors.append('Missing artwork: '+c['cardName'])

    profile=Path(__file__).with_name('supported-rules.json')

    if profile.exists():

        expected=json.loads(profile.read_text(encoding='utf-8'))

        for d in data['decks']:

            for e in d['main']+d['evolution']:

                c=e['card']; name=c['cardName']

                if name in expected and rule_signature(c)!=expected[name]: errors.append('Rules changed; update/test adapter before simulating: '+name)

    return errors



def rule_signature(c):

    keys=('abilities','promoteCondition','arrowEffect','guardArrowEffect','triumphQuest','onEvolution','evolutionOngoing')

    return {k:c.get(k) for k in keys}



def rotate(mask): return ((mask<<2)|(mask>>6)) & 255



@dataclass(eq=False)

class Unit:

    card: dict

    owner: int

    col: int

    row: int=1

    damage: int=0

    ready: bool=False

    guard: bool=False

    scars: int=0

    power_temp: int=0

    defense_temp: int=0

    next_power: int=0

    web_skip: bool=False

    used: bool=False

    @property

    def name(self): return self.card['cardName']



class Game:

    def __init__(self,a,b,seed,rules=None):

        self.rules=dict(RULES,**(rules or {})); self.rng=random.Random(seed)

        self.decks=[a,b]; self.units=[]; self.hands=[[],[]]; self.piles=[[],[]]

        self.life=[self.rules['life']]*2; self.web=set(); self.occupied=set()

        self.metrics=collections.Counter(); self.events=[]; self.frames=[]; self.capture=False; self.evolved=set(); self.turn=0; self.active=0; self.in_battle=False

        for p,d in enumerate(self.decks):

            pile=[copy.deepcopy(e['card']) for e in d['main'] for _ in range(e['quantity'])]

            self.rng.shuffle(pile); self.piles[p]=pile

            for _ in range(self.rules['hand']): self.draw(p)

    def log(self,text):

        self.events.append(f'T{self.turn+1} P{self.active+1}: '+text)

        if self.capture:

            self.frames.append(dict(event=self.events[-1],life=self.life[:],units=[dict(name=u.name,owner=u.owner,row=u.row,col=u.col,damage=u.damage,scars=u.scars,ready=u.ready,guard=u.guard,arrows=rotate(u.card['attackArrows']) if u.guard else u.card['attackArrows'],stats=self.stats(u)) for u in self.units]))

    def at(self,p,row,col): return next((u for u in self.units if (u.owner,u.row,u.col)==(p,row,col)),None)

    def own(self,p): return [u for u in self.units if u.owner==p]

    def draw(self,p):

        if not self.piles[p]: self.life[p]=0; self.metrics['deck_outs']+=1; return

        self.hands[p].append(self.piles[p].pop())

    def arrows(self,u):

        if not self.rules['arrows']: return set()

        mask=rotate(u.card['attackArrows']) if u.guard else u.card['attackArrows']

        gy=u.row if u.owner==0 else 3-u.row; facing=1 if u.owner==0 else -1

        targets=set()

        for i,(dx,dy) in enumerate(DELTAS):

            if not (mask & (1<<i)): continue

            x=u.col+dx*facing; y=gy+dy*facing

            if 0<=x<5 and 0<=y<4:

                p=0 if y<2 else 1; row=y if p==0 else 3-y; targets.add((p,row,x))

        return targets

    def points(self,a,b): return (b.owner,b.row,b.col) in self.arrows(a)

    def stats(self,u,attacking=False):

        power=u.card['power']+u.power_temp+u.next_power; defence=u.card['defense']+u.defense_temp

        for s in self.units:

            if s is u or not self.points(s,u): continue

            if s.owner==u.owner and s.name in ('Briar Warden','Heartwood Herald'):

                if s.guard: defence+=1

                else: power+=1

            if s.owner==u.owner and s.name=='Broodgate Demon' and s.guard: defence+=1

            if s.owner!=u.owner and s.name=='Silkspinner' and attacking: power-=1

            if s.owner!=u.owner and s.name=='Broodgate Demon' and not s.guard and s.owner==self.active and self.in_battle: defence-=1

        return max(0,power),max(1,defence)

    def heal(self,p,candidates=None):

        options=[u for u in (candidates if candidates is not None else self.own(p)) if u.damage>0]

        if options:

            u=max(options,key=lambda x:x.damage); u.damage-=1; self.metrics['heals']+=1

    def enter(self,u,row,col,moved=False):

        old=(u.owner,u.row,u.col); changed=old!=(u.owner,row,col)

        u.row=row; u.col=col

        if (u.owner,col) in self.web and row==1:

            self.web.remove((u.owner,col)); u.ready=False; u.web_skip=True; self.metrics['web_entries']+=1

        if moved and changed:

            self.metrics['moves']+=1; self.log(f'{u.name} moves to {"front" if row else "rear"} {col+1}')

            self.left(old)

            for q in self.own(u.owner):

                if q.name=='Queen of the Unclaimed Path' and not q.used and row==1 and self.points(q,u):

                    u.next_power=max(u.next_power,1); q.used=True; self.metrics['queen_arrow_boosts']+=1

            for s in list(self.own(u.owner)):

                if s is not u and s.name=='Ruin Spectre' and s.row==1 and not s.used:

                    empty=[x for x in (s.col-1,s.col+1) if 0<=x<5 and self.at(s.owner,1,x) is None]

                    if empty: s.used=True; self.enter(s,1,self.best_lane(s.owner,empty),True)

    def left(self,old):

        p,row,col=old

        for s in list(self.units):

            if s.owner!=p and s.name=='Widow Matron' and old in self.arrows(s): self.heal(s.owner)

            if s.owner!=p and row==1 and s.col==col:

                if s.name=='Grave Moth': s.next_power=max(1,s.next_power)

                if s.name=='Hollow Skitter' and s.row==0 and self.at(s.owner,1,col) is None:

                    s.ready=False; self.enter(s,1,col,True); self.metrics['promotions']+=1

    def best_lane(self,p,cols):

        # Current-board heuristic only; no access to opposing hand or future draws.

        def value(col):

            enemy=self.at(1-p,1,col); neighbours=sum(abs(u.col-col)<=1 for u in self.own(p) if u.row==1)

            return (3 if enemy is None else 0)+neighbours*.7-abs(2-col)*.1+self.rng.random()*.2

        return max(cols,key=value)

    def play(self,p,c,row,col):

        self.hands[p].remove(c); u=Unit(c,p,col,row); self.units.append(u); self.enter(u,row,col)

        self.metrics['plays']+=1; self.log(f'plays {u.name} in {"front" if row else "rear"} {col+1}')

        if u.name=='Shade Imp' and row==1:

            target=self.at(1-p,1,col)

            if target: target.power_temp-=1

        if u.name=='Webling':

            choices=[x for x in (col-1,col+1) if 0<=x<5]

            if choices: self.web.add((1-p,self.rng.choice(choices)))

        if u.name=='Vine Scout':

            for other in self.own(p):

                if other.row!=0: continue

                choices=[x for x in (other.col-1,other.col+1) if 0<=x<5 and self.at(p,0,x) is None]

                if choices: self.enter(other,0,self.best_lane(p,choices),True); break

        return u

    def rescue(self,u,old_damage):

        for s in self.own(u.owner):

            if s is not u and s.name=='Yggdrasil, Living Citadel' and not s.used and self.points(s,u):

                empty=[x for x in range(5) if self.at(u.owner,0,x) is None]

                if empty:

                    # Explicit prototype ruling: prevent this lethal packet, then relocate.

                    u.damage=old_damage; s.used=True; u.ready=False

                    self.enter(u,0,min(empty,key=lambda x:abs(x-u.col)),True); self.metrics['citadel_saves']+=1; return True

        return False

    def battle(self,a,d):

        self.metrics['battles']+=1; a.ready=False; self.in_battle=True

        guards=[g for g in self.own(1-a.owner) if g is not d and g.row==1 and g.guard and g.ready and g.card['guard'] and abs(g.col-a.col)==1]

        protected=False

        if guards:

            d=max(guards,key=lambda u:self.stats(u)[1]-u.damage); d.ready=False; protected=True; self.metrics['intercepts']+=1

        if d is None:

            self.life[1-a.owner]-=self.stats(a,True)[0]; a.next_power=0; self.metrics['direct_hits']+=1

            if (1-a.owner,a.col) in self.occupied: self.evolve(a.owner,True)

            self.log(f'{a.name} strikes player through column {a.col+1}')

            self.in_battle=False

            self.evolve(1-a.owner,False)

            return

        ap=self.stats(a,True)[0]; dp=self.stats(d)[0]

        ad=self.stats(a)[1]; dd=self.stats(d)[1]+(1 if protected and d.name=='Walking Thicket' else 0)

        old={a:a.damage,d:d.damage}; a.damage+=dp; d.damage+=ap; a.next_power=d.next_power=0

        self.log(f'{a.name} battles {d.name} in column {d.col+1}: {ap}/{dp} damage')

        dead=[]; survivors=[]

        for u,limit,taken in ((a,ad,dp),(d,dd,ap)):

            if u.damage>=limit:

                if not self.rescue(u,old[u]): dead.append(u)

            elif taken>0: u.scars+=1; survivors.append(u); self.metrics['scars']+=1

        # Remove simultaneous casualties before emitting vacancy triggers.

        for u in dead:

            if u in self.units: self.units.remove(u); self.metrics['deaths']+=1

        for u in dead: self.left((u.owner,u.row,u.col))

        for survivor in survivors:

            for s in list(self.own(survivor.owner)):

                if s is survivor: continue

                if s.name=='Seedling Attendant' and abs(s.col-survivor.col)+abs(s.row-survivor.row)==1:

                    self.heal(s.owner,[survivor])

                if s.name=='Briar Sprout' and s.row==0 and abs(s.col-survivor.col)==1 and self.at(s.owner,1,s.col) is None:

                    s.ready=False; survivor.defense_temp+=1; self.enter(s,1,s.col,True); self.metrics['promotions']+=1

        self.in_battle=False

        self.evolve(1-a.owner,False)

    def evolve(self,p,breakthrough):

        evo=self.decks[p]['evolution'][0]; c=evo['card']

        if p in self.evolved: return

        base=next((u for u in self.own(p) if u.name==evo['evolvesFrom'] and u.scars>=self.rules['scars']),None)

        if base is None: return

        columns=None

        if c['cardName']=='Yggdrasil, Living Citadel':

            if p==self.active: return

            columns=next((set(range(start,start+3)) for start in range(3) if all(any(u.col==x for u in self.own(p)) for x in range(start,start+3)) and sum(u.row==1 and start<=u.col<start+3 for u in self.own(p))>=2),None)

            if columns is None: return

        elif not breakthrough: return

        base.card=copy.deepcopy(c); self.evolved.add(p); self.metrics['evolutions_'+str(p)]+=1; self.log('Triumph: '+base.name)

        if columns:

            for u in self.own(p):

                if u.col in columns and u.damage: u.damage-=1; self.metrics['heals']+=1

        else:

            for u in list(self.own(p)):

                if u.row!=1: continue

                choices=[x for x in (u.col-1,u.col+1) if 0<=x<5 and self.at(p,1,x) is None]

                if choices: self.enter(u,1,self.best_lane(p,choices),True); break

    def step(self,p):

        self.active=p; self.occupied={(u.owner,u.col) for u in self.units if u.row==1}

        for u in self.units: u.used=False

        for u in self.own(p):

            u.ready=not u.web_skip; u.web_skip=False

        if self.turn or self.rules['draw_first']: self.draw(p)

        if self.life[p]<=0: return

        # A free rear deployment is the explicit prototype resource bootstrap.

        empty=[x for x in range(5) if self.at(p,0,x) is None]

        rear_options=[c for c in self.hands[p] if c['canPlayInRear']]

        if empty and rear_options and sum(u.row==0 for u in self.own(p))<self.rules['rear_cap']:

            c=min(rear_options,key=lambda c:(c['grade'],c['power'])); self.play(p,c,0,self.best_lane(p,empty))

        for _ in range(self.rules['actions']):

            mana=[u for u in self.own(p) if u.row==0 and u.ready]

            empty=[x for x in range(5) if self.at(p,1,x) is None]

            options=[c for c in self.hands[p] if c['canPlayInFront'] and len(mana)>=1+c['genericEssenceCost'] and any(u.card['rearProduces']==c['essence'] for u in mana)]

            if empty and options:

                c=max(options,key=lambda c:c['grade']*3+c['power']+c['defense']*.4)

                paid=[next(u for u in mana if u.card['rearProduces']==c['essence'])]

                paid += [u for u in mana if u not in paid][:c['genericEssenceCost']]

                for u in paid: u.ready=False

                self.play(p,c,1,self.best_lane(p,empty)); continue

            # One sideways manoeuvre or stance change consumes an action.

            movers=[u for u in self.own(p) if u.row==1 and u.ready and not u.guard]

            moved=False

            for u in sorted(movers,key=lambda x:-self.stats(x,True)[0]):

                cols=[x for x in (u.col-1,u.col+1) if 0<=x<5 and self.at(p,1,x) is None and self.at(1-p,1,x) is None]

                if cols and self.at(1-p,1,u.col): self.enter(u,1,self.best_lane(p,cols),True); moved=True; break

            if moved: continue

            for u in self.own(p):

                desired=u.card['guard'] and any(abs(e.col-u.col)<=1 and e.row==1 for e in self.own(1-p))

                if u.name=='Broodgate Demon': desired=desired and u.damage>=3

                if u.row==1 and u.guard!=desired:

                    u.guard=desired; self.metrics['stance_changes']+=1; moved=True; break

            if not moved: break

        for u in self.own(p):

            if u.name=='Grove Mender' and not u.used:

                self.heal(p,[v for v in self.own(p) if abs(v.col-u.col)<=1]); u.used=True

        attackers=[u for u in self.own(p) if u.row==1 and u.ready and not u.guard]

        self.rng.shuffle(attackers)

        for u in attackers:

            if u not in self.units or self.life[1-p]<=0: break

            self.battle(u,self.at(1-p,1,u.col))

        for u in self.units: u.power_temp=u.defense_temp=0

        # Losing temporary defence can expose lethal persistent damage.

        for u in list(self.units):

            if u.damage>=self.stats(u)[1]: self.units.remove(u); self.left((u.owner,u.row,u.col)); self.metrics['deaths']+=1

        self.log('End turn')

    def run(self):

        for self.turn in range(self.rules['max_turns']):

            self.step(self.turn%2)

            if min(self.life)<=0: break

        winner=-1 if (self.life[0]<=0)==(self.life[1]<=0) else (0 if self.life[1]<=0 else 1)

        self.metrics['turns']=self.turn+1

        return winner



def series(a,b,pairs,seed,rules=None,trace=False):

    wins=[0,0]; draws=0; first=0; metrics=collections.Counter(); scores=[]; evo=[0,0]; example=[]; frames=[]

    for i in range(pairs):

        pair_score=0

        for swap in (False,True):

            g=Game(b if swap else a,a if swap else b,seed+i,rules);g.capture=trace and i==0 and not swap;w=g.run()

            if w==-1: draws+=1; pair_score+=.5

            else:

                mapped=1-w if swap else w; wins[mapped]+=1; pair_score+=int(mapped==0); first+=int(w==0)

            for seat in (0,1): evo[1-seat if swap else seat]+=g.metrics['evolutions_'+str(seat)]

            metrics.update(g.metrics)

            if trace and i==0 and not swap: example=g.events;frames=g.frames

        scores.append(pair_score/2)

    mean=sum(scores)/pairs

    # Cluster on paired seeds, because swapped-seat games are correlated.

    se=math.sqrt(sum((x-mean)**2 for x in scores)/(pairs-1)/pairs) if pairs>1 else 0

    return dict(decks=[a['name'],b['name']],games=2*pairs,wins=wins,draws=draws,score=mean,

                paired_95_interval=[max(0,mean-1.96*se),min(1,mean+1.96*se)],first_player_wins=first,

                evolutions=evo,metrics=dict(metrics),trace=example,frames=frames)



def hypergeom_no(n,k,h): return math.comb(n-k,h)/math.comb(n,h) if n-k>=h else 0



def report(data,out,results):

    out=Path(out); out.mkdir(parents=True,exist_ok=True)

    esc=lambda v:html.escape(str(v))

    body=['<h1>Five Roads · Deck laboratory</h1><p>Authored fields, artwork and reproducible prototype tests. Simulation results are conditional on the documented rules and bot; they are not human-play win rates.</p>']

    body.append('<h2>Matchups</h2><p><a href="Battle-replay.html">Open the board replay</a> · <a href="README.txt">Rules, assumptions and test instructions</a></p><table><tr><th>Matchup</th><th>Games</th><th>Wins / draws / losses</th><th>First deck score</th><th>Paired 95% interval</th></tr>')

    for r in results:

        lo,hi=r['paired_95_interval']

        body.append(f'<tr><td>{esc(" vs ".join(r["decks"]))}</td><td>{r["games"]}</td><td>{r["wins"][0]} / {r["draws"]} / {r["wins"][1]}</td><td>{r["score"]:.1%}</td><td>{lo:.1%}–{hi:.1%}</td></tr>')

    body.append('</table><p>Full metrics are in matchups.json. AUTO = triggered, ACT = activated, CONT = continuous. Numbered effects borrow OCG organisation; punctuation borrows PSCT. This prototype does not implement a Yu-Gi-Oh chain system.</p>')

    for d in data['decks']:

        total=sum(e['quantity'] for e in d['main']); low=sum(e['quantity'] for e in d['main'] if e['card']['grade']==1)

        body.append(f'<h2>{esc(d["name"])}</h2><p>{total} main cards; {low} Grade 1. Chance of no Grade 1 in an opening five: {hypergeom_no(total,low,5):.2%}. Deck copy counts exclude alternate art variants.</p>')

        for e in d['main']+d['evolution']:

            c=e['card']; name=c['cardName']; src='cards/'+name+'-Attack.png'

            body.append('<article><img loading="lazy" src="'+esc(src)+'"><div><h3>'+esc(name)+' × '+str(e['quantity'])+'</h3>')

            body.append('<p>Artwork: '+esc(e['artworkPath'])+'<br>Background: '+esc(e['backgroundPath'] or 'Essence default')+'<br>Evolution base: '+esc(e['evolvesFrom'] or 'None')+'</p>')

            directions=['N','NE','E','SE','S','SW','W','NW']
            arrows=', '.join(directions[i] for i in range(8) if c['attackArrows'] & (1<<i)) or 'None'
            body.append(f'<p><strong>Grade {c["grade"]} · Power {c["power"]} / Defence {c["defense"]}</strong><br>Attack arrows: {arrows}</p>')
            effects=[]
            if c['guard']: effects.append('Guard')
            if c['promote']: effects.append('Promote: '+c['promoteCondition'])
            for key,label in [('arrowEffect','Attack arrows'),('guardArrowEffect','Guard arrows'),('triumphQuest','Triumph quest'),('onEvolution','On evolution'),('evolutionOngoing','Ongoing')]:
                if c.get(key): effects.append(label+': '+c[key])
            for ability in c.get('abilities',[]): effects.append(('Once per turn: ' if ability.get('oncePerTurn') else '')+ability.get('effect',''))
            body.append('<ul>'+''.join('<li>'+esc(x)+'</li>' for x in effects)+'</ul><details><summary>All Unity fields</summary>')
            body.append('<table>'+''.join('<tr><th>'+esc(k)+'</th><td>'+esc(json.dumps(v,ensure_ascii=False) if isinstance(v,(dict,list)) else v)+'</td></tr>' for k,v in c.items() if k not in ('m_Name','m_EditorClassIdentifier'))+'</table></details></div></article>')

    body.append('<h2>Alternate art assets (not extra deck copies)</h2><pre>'+esc(json.dumps(data.get('variants',[]),ensure_ascii=False,indent=2))+'</pre>')

    (out/'Deck-breakdown.html').write_text('<!doctype html><meta charset="utf-8"><title>Deck laboratory</title><style>body{background:#162127;color:#e8eee8;font:16px system-ui;max-width:1300px;margin:40px auto;padding:20px}h1,h2,h3{color:#d78fa8}article{display:grid;grid-template-columns:300px 1fr;gap:30px;padding:28px 0;border-top:1px solid #516069}img{width:300px;position:sticky;top:20px}table{border-collapse:collapse;width:100%;font-size:14px}td,th{border-bottom:1px solid #34434b;padding:8px;text-align:left;overflow-wrap:anywhere}pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:13px} @media(max-width:750px){article{display:block}img{position:static}}</style>'+''.join(body),encoding='utf-8')

    (out/'matchups.json').write_text(json.dumps(results,indent=2),encoding='utf-8')

    if results:

        replay=next((r for r in results if r.get('frames')),results[0])

        (out/'example-battle.txt').write_text('\n'.join(replay.get('trace',[])),encoding='utf-8')

        template=Path(__file__).with_name('replay-template.html').read_text(encoding='utf-8')

        (out/'Battle-replay.html').write_text(template.replace('/*DATA*/',json.dumps(replay.get('frames',[])).replace('<','\\u003c')),encoding='utf-8')



def main():

    p=argparse.ArgumentParser(); p.add_argument('data'); p.add_argument('--out',default='reports'); p.add_argument('--pairs',type=int,default=200); p.add_argument('--seed',type=int,default=RULES['seed']); p.add_argument('--rules'); p.add_argument('--sweep',action='store_true')

    args=p.parse_args()

    if args.pairs<2: p.error('--pairs must be at least 2')

    data=load(args.data); errors=validate(data)

    if errors: raise SystemExit('\n'.join(errors))

    rules=json.loads(Path(args.rules).read_text()) if args.rules else dict(RULES)

    decks=sorted(data['decks'],key=lambda d:d['name']!='Rootbound Court'); results=[]

    for i,a in enumerate(decks):

        for b in decks[i:]: results.append(series(a,b,args.pairs,args.seed,rules,trace=a['name']!=b['name']))

    report(data,args.out,results)

    manifest={'data_sha256':hashlib.sha256(Path(args.data).read_bytes()).hexdigest(),'rules':rules,'pairs':args.pairs,'seed':args.seed,'scope':'Experimental complete named-card adapters; no arbitrary natural-language rule execution.'}

    Path(args.out,'run-manifest.json').write_text(json.dumps(manifest,indent=2))

    if args.sweep and len(decks)>=2:

        candidates=[]

        # Shadow profiles only: authored JSON and Unity assets are never overwritten.

        for delta in (-1,1):

            candidate=copy.deepcopy(decks[1])

            for e in candidate['main']:

                if e['card']['grade']==2: e['card']['defense']=max(1,e['card']['defense']+delta)

            result=series(decks[0],candidate,args.pairs,args.seed,rules)

            candidates.append({'proposal':f'{candidate["name"]} Grade 2 defence {delta:+d}','result':result})

        Path(args.out,'candidate-comparisons.json').write_text(json.dumps(candidates,indent=2))

    for r in results: print(r['decks'],r['wins'],'draws',r['draws'],'score',round(r['score'],3),'95% paired interval',r['paired_95_interval'])



if __name__=='__main__': main()

