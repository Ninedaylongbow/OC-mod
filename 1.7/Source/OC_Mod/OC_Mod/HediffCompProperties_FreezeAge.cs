using System;
using RimWorld;
using Verse;

namespace OC_Mod
{
    // XML에서 설정값을 받아오기 위한 Properties 클래스
    public class HediffCompProperties_FreezeAge : HediffCompProperties
    {
        // true: 매 틱마다 나이를 고정하여 영원히 늙지 않음
        // false: 헤디프가 추가될 때 딱 한 번만 나이를 바꾸고 이후론 정상적으로 늙음
        public bool permanent = true;

        // 고정/변경할 나이 (단위: 년)
        // -1로 설정하면 헤디프가 부여된 순간의 현재 나이를 그대로 사용합니다.
        public float targetAgeYears = -1f;

        public HediffCompProperties_FreezeAge()
        {
            this.compClass = typeof(HediffComp_FreezeAge);
        }
    }

    // 실제 동작을 담당하는 Comp 클래스
    public class HediffComp_FreezeAge : HediffComp
    {
        private long frozenAgeTicks = -1;

        // Properties 값에 쉽게 접근하기 위한 프로퍼티
        public HediffCompProperties_FreezeAge Props => (HediffCompProperties_FreezeAge)this.props;

        /// <summary>
        /// 헤디프가 폰에게 처음 부여되는 순간 1회 실행
        /// </summary>
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            if (this.Pawn != null && this.Pawn.ageTracker != null)
            {
                // XML에서 특정 나이(targetAgeYears)를 지정했다면 해당 나이로 틱을 계산
                // (림월드에서 1년은 3,600,000 틱입니다)
                if (Props.targetAgeYears >= 0)
                {
                    this.frozenAgeTicks = (long)(Props.targetAgeYears * 3600000f);
                }
                else
                {
                    // 특정 나이를 지정하지 않았다면 현재 생체 나이를 기록
                    this.frozenAgeTicks = this.Pawn.ageTracker.AgeBiologicalTicks;
                }

                // 부여 즉시 폰의 나이를 설정 (한 번만 적용이든 영구 적용이든 일단 덮어씌움)
                this.Pawn.ageTracker.AgeBiologicalTicks = this.frozenAgeTicks;
                this.Pawn.ageTracker.AgeChronologicalTicks = this.frozenAgeTicks;
            }
        }

        /// <summary>
        /// 매 틱(초당 60회)마다 실행
        /// </summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // 영구 고정(permanent) 설정이 true일 때만 나이가 먹지 않도록 매 틱마다 덮어씌움
            if (Props.permanent && this.Pawn != null && this.Pawn.ageTracker != null)
            {
                // 세이브 로드 등 예외 상황에서 값이 날아갔을 경우를 대비한 안전장치
                if (this.frozenAgeTicks == -1)
                {
                    this.frozenAgeTicks = this.Pawn.ageTracker.AgeBiologicalTicks;
                }

                this.Pawn.ageTracker.AgeBiologicalTicks = this.frozenAgeTicks;
            }
        }

        /// <summary>
        /// 세이브/로드 시 데이터 유지
        /// </summary>
        public override void CompExposeData()
        {
            base.CompExposeData();
            // 영구 고정일 경우 기준 나이를 기억해야 하므로 세이브 파일에 저장합니다.
            Scribe_Values.Look(ref this.frozenAgeTicks, "frozenAgeTicks", -1);
        }
    }
}